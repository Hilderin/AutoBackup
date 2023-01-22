using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace AutoBackup
{
    /// <summary>
    /// Helper for the encryption
    /// </summary>
    public static class CryptoHelper
    {
        /// <summary>
        /// Cache algo
        /// </summary>
        private static Dictionary<string, RijndaelManaged> _cacheAlgo = new Dictionary<string, RijndaelManaged>();

        /// <summary>
        /// Create an algo from a password
        /// </summary>
        public static CryptoAlgo CreateAlgo(string password)
        {

            //convert password string to byte arrray
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);


            //generate random salt
            byte[] salt = GenerateRandomSalt();

            //Set Rijndael symmetric encryption algorithm
            RijndaelManaged algo = new RijndaelManaged();

            algo.KeySize = 256;
            algo.BlockSize = 128;
            algo.Padding = PaddingMode.PKCS7;

            //http://stackoverflow.com/questions/2659214/why-do-i-need-to-use-the-rfc2898derivebytes-class-in-net-instead-of-directly
            //"What it does is repeatedly hash the user password along with the salt." High iteration counts.
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            algo.Key = key.GetBytes(algo.KeySize / 8);
            algo.IV = key.GetBytes(algo.BlockSize / 8);

            //Cipher modes: http://security.stackexchange.com/questions/52665/which-is-the-best-cipher-mode-and-padding-mode-for-aes-encryption
            algo.Mode = CipherMode.CFB;

            return new CryptoAlgo() { Algo = algo, Salt = salt };

        }

        /// <summary>
        /// Encrypts a file from its path and a plain password.
        /// </summary>
        public static void FileEncrypt(string inputFile, string outputFile, CryptoAlgo algo)
        {
            //http://stackoverflow.com/questions/27645527/aes-encryption-on-large-files


            //create output file name
            using (FileStream fsCrypt = new FileStream(outputFile, FileMode.Create))
            {

                // write salt to the begining of the output file, so in this case can be random every time
                fsCrypt.Write(algo.Salt, 0, algo.Salt.Length);

                using (CryptoStream cs = new CryptoStream(fsCrypt, algo.Algo.CreateEncryptor(), CryptoStreamMode.Write))
                {

                    using (FileStream fsIn = new FileStream(inputFile, FileMode.Open))
                    {
                        fsIn.CopyTo(cs);
                    }
                }
            }
        }


        /// <summary>
        /// Decrypts an encrypted file with the FileEncrypt method through its path and the plain password.
        /// </summary>
        public static void FileDecrypt(string inputFile, string outputFile, string password)
        {
            
            byte[] salt = new byte[32];

            using (FileStream fsCrypt = new FileStream(inputFile, FileMode.Open))
            {
                fsCrypt.Read(salt, 0, salt.Length);

                RijndaelManaged algo;
                string algoKey = password.GetHashCode().ToString() + BitConverter.ToString(salt);
                if (!_cacheAlgo.TryGetValue(algoKey, out algo))
                {
                    byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                    algo = new RijndaelManaged();
                    
                    algo.KeySize = 256;
                    algo.BlockSize = 128;
                    var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
                    algo.Key = key.GetBytes(algo.KeySize / 8);
                    algo.IV = key.GetBytes(algo.BlockSize / 8);
                    algo.Padding = PaddingMode.PKCS7;
                    algo.Mode = CipherMode.CFB;

                    _cacheAlgo.Add(algoKey, algo);
                }

                using (CryptoStream cs = new CryptoStream(fsCrypt, algo.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (FileStream fsOut = new FileStream(outputFile, FileMode.Create))
                    {
                        cs.CopyTo(fsOut);
                    }
                }
                
            }
        }


        /// <summary>
        /// Creates a random salt that will be used to encrypt your file. This method is required on FileEncrypt.
        /// </summary>
        /// <returns></returns>
        private static byte[] GenerateRandomSalt()
        {
            byte[] data = new byte[32];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                for (int i = 0; i < 10; i++)
                {
                    // Fille the buffer with the generated data
                    rng.GetBytes(data);
                }
            }

            return data;
        }
    }

    /// <summary>
    /// Information on the crypto algo
    /// </summary>
    public class CryptoAlgo
    {
        public RijndaelManaged Algo;
        public byte[] Salt;
    }
}
