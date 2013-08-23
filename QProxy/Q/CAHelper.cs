using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Q.Configuration;

namespace Q
{
    internal static class CAHelper
    {
        private const string MAKE_CERT_PARAMS_ROOT = "-r -ss root -n \"CN=QProxy, OU=Loye\" -sky signature -cy authority -a sha1 -m 120";
        private const string MAKE_CERT_PARAMS_END = "-pe -ss my -n \"CN={0}, OU=Loye\" -sky exchange -in \"QProxy\" -is root -cy end -a sha1 -m 120";
        private const string MAKE_CERT_SUBJECT = "CN={0}, OU=Loye";
        private const string MAKE_CERT_ROOT_DOMAIN = "QProxy";

        private static readonly string MAKECERT_FILENAME = @".\makecert.exe";
        private static readonly ConcurrentDictionary<string, X509Certificate2> certificateCache = new ConcurrentDictionary<string, X509Certificate2>();
        private static X509Certificate2 rootCert;
        private static readonly ReaderWriterLockSlim caRWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public static X509Certificate2 GetCertificate(string host)
        {
            if (String.IsNullOrEmpty(host))
            {
                throw new Exception("Create Certification: Failed. Host can't be null or empty");
            }
            if (certificateCache.ContainsKey(host))
            {
                return certificateCache[host];
            }

            X509Certificate2 domainCert = LoadCertificateFromWindowsStore(host);
            if (domainCert == null)
            {
                domainCert = CreateCertificate(host);
            }
            if (domainCert != null)
            {
                certificateCache[host] = domainCert;
            }
            return domainCert;
        }

        private static X509Certificate2 LoadCertificateFromWindowsStore(string host, StoreName storeName = StoreName.My)
        {
            X509Store x509Store = new X509Store(storeName, StoreLocation.CurrentUser);
            try
            {
                caRWLock.EnterReadLock();
                x509Store.Open(OpenFlags.ReadOnly);
                string subject = String.Format(MAKE_CERT_SUBJECT, host);
                foreach (X509Certificate2 cert in x509Store.Certificates)
                {
                    if (String.Equals(cert.Subject, subject, StringComparison.OrdinalIgnoreCase))
                    {
                        x509Store.Close();
                        return cert;
                    }
                }
            }
            finally
            {
                if (x509Store != null)
                {
                    x509Store.Close();
                }
                caRWLock.ExitReadLock();
            }
            return null;
        }

        private static X509Certificate2 CreateCertificate(string host, bool isRoot = false)
        {
            if (String.IsNullOrEmpty(MAKECERT_FILENAME) || !File.Exists(MAKECERT_FILENAME))
            {
                throw new Exception("Create Certification: Failed. Can't find makecert.exe");
            }
            X509Certificate2 cert = null;
            if (!isRoot)
            {
                if (rootCert == null)
                {
                    rootCert = LoadCertificateFromWindowsStore(MAKE_CERT_ROOT_DOMAIN, StoreName.Root);
                    if (rootCert == null)
                    {
                        rootCert = CreateCertificate(MAKE_CERT_ROOT_DOMAIN, true);
                        if (rootCert == null)
                        {
                            throw new Exception("Create Certification: Failed. Can't find root certification");
                        }
                    }
                }
            }
            int exitCode = 999;
            string execute = MAKECERT_FILENAME;
            string parameters = isRoot ? MAKE_CERT_PARAMS_ROOT : String.Format(MAKE_CERT_PARAMS_END, host);
            try
            {
                caRWLock.EnterWriteLock();
                cert = LoadCertificateFromWindowsStore(host);
                if (cert != null)
                {
                    return cert;
                }
                using (Process process = new Process())
                {
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.FileName = execute;
                    process.StartInfo.Arguments = parameters;
                    process.Start();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }
            }
            finally
            {
                caRWLock.ExitWriteLock();
            }
            if (exitCode == 0)
            {
                cert = LoadCertificateFromWindowsStore(host);
            }
            if (cert == null)
            {
                throw new Exception("Create Certification: Failed.");
            }
            return cert;
        }
    }
}
