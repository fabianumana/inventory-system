using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace SCS.Models
{
    public class Encriptador
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 10000;

        //Este metodo hashea la contraseña
        public static string HashPassword(string password)
        {

            var salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }


            var hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Iterations,
                numBytesRequested: HashSize
            );

            var saltedHash = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, saltedHash, 0, SaltSize);
            Array.Copy(hash, 0, saltedHash, SaltSize, HashSize);

            return Convert.ToBase64String(saltedHash);
        }

        //Este metodo ayuda a verificar la contraseña hasheada
        public static bool VerifyPassword(string hashedPassword, string password)
        {
            byte[] saltedHash;
            try
            {
                saltedHash = Convert.FromBase64String(hashedPassword);
            }
            catch (FormatException)
            {
                throw new ArgumentException("The hashed password is not a valid base64 string.");
            }

            if (saltedHash.Length != SaltSize + HashSize)
            {
                throw new ArgumentException("Invalid salted hash format.");
            }

            var salt = new byte[SaltSize];
            var hash = new byte[HashSize];
            Array.Copy(saltedHash, 0, salt, 0, SaltSize);
            Array.Copy(saltedHash, SaltSize, hash, 0, HashSize);

            var newHash = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Iterations,
                numBytesRequested: HashSize
            );

            for (int i = 0; i < HashSize; i++)
            {
                if (hash[i] != newHash[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
