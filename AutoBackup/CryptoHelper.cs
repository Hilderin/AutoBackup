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
        /// Encrypts a file from its path and a plain password.
        /// </summary>
        public static void FileEncrypt(string inputFile, string outputFile, string password)
        {
            //http://stackoverflow.com/questions/27645527/aes-encryption-on-large-files

            //generate random salt
            byte[] salt = GenerateRandomSalt();

            //create output file name
            using (FileStream fsCrypt = new FileStream(outputFile, FileMode.Create))
            {

                //convert password string to byte arrray
                byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

                //Set Rijndael symmetric encryption algorithm
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    AES.Padding = PaddingMode.PKCS7;

                    //http://stackoverflow.com/questions/2659214/why-do-i-need-to-use-the-rfc2898derivebytes-class-in-net-instead-of-directly
                    //"What it does is repeatedly hash the user password along with the salt." High iteration counts.
                    var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    //Cipher modes: http://security.stackexchange.com/questions/52665/which-is-the-best-cipher-mode-and-padding-mode-for-aes-encryption
                    AES.Mode = CipherMode.CFB;

                    // write salt to the begining of the output file, so in this case can be random every time
                    fsCrypt.Write(salt, 0, salt.Length);

                    using (CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {

                        using (FileStream fsIn = new FileStream(inputFile, FileMode.Open))
                        {
                            fsIn.CopyTo(cs);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Decrypts an encrypted file with the FileEncrypt method through its path and the plain password.
        /// </summary>
        public static void FileDecrypt(string inputFile, string outputFile, string password)
        {
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] salt = new byte[32];

            using (FileStream fsCrypt = new FileStream(inputFile, FileMode.Open))
            {
                fsCrypt.Read(salt, 0, salt.Length);

                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);
                    AES.Padding = PaddingMode.PKCS7;
                    AES.Mode = CipherMode.CFB;

                    using (CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (FileStream fsOut = new FileStream(outputFile, FileMode.Create))
                        {
                            cs.CopyTo(fsOut);
                        }
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
}
