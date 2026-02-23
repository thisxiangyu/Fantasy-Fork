#if FANTASY_NET
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Fantasy.Product.Authentication
{
    /// <summary>
    /// 加盐加密帮助类, 用于增强密码等敏感信息的安全性
    /// </summary>
    public partial class SaltPasswordHelper
    {
        const byte CurrentVersion = 0x01; // 当前版本号

        // ==== Argon2参数 ====
        struct Argon2Param
        {
            public int SaltSize;
            public int HashSize;
            public int Iterations;
            public int MemorySize;
            public int DegreeOfParallelism;
        }
        static Argon2Param GetArgon2ParamByVersion(byte version)
        {
            return version switch
            {
                0x01 => new Argon2Param
                {
                    SaltSize = 16,
                    HashSize = 32,
                    Iterations = 2,
                    MemorySize = 32768,
                    DegreeOfParallelism = 2
                },
                // Note : 未来可以增加别的版本

                _ => throw new InvalidOperationException($"未知版本: {version}")
            };
        }

        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("密码不能为空");

            var param = GetArgon2ParamByVersion(CurrentVersion);

            byte[] salt = RandomNumberGenerator.GetBytes(param.SaltSize);
            byte[] hash = Hash(password, salt, param);

            byte[] result = new byte[1 + param.SaltSize + param.HashSize];

            result[0] = CurrentVersion;
            Buffer.BlockCopy(salt, 0, result, 1, param.SaltSize);
            Buffer.BlockCopy(hash, 0, result, 1 + param.SaltSize, param.HashSize);

            return Convert.ToBase64String(result);
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            byte[] decoded = Convert.FromBase64String(storedHash);
            if (decoded.Length < 1) return false;

            byte version = decoded[0];

            var param = GetArgon2ParamByVersion(version);

            if (decoded.Length != 1 + param.SaltSize + param.HashSize)
                return false;

            byte[] salt = new byte[param.SaltSize];
            byte[] storedSubHash = new byte[param.HashSize];

            Buffer.BlockCopy(decoded, 1, salt, 0, param.SaltSize);
            Buffer.BlockCopy(decoded, 1 + param.SaltSize, storedSubHash, 0, param.HashSize);

            byte[] computedHash = Hash(password, salt, param);

            return CryptographicOperations.FixedTimeEquals(storedSubHash, computedHash);
        }

        static byte[] Hash(string password, byte[] salt, Argon2Param param)
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                Iterations = param.Iterations,
                MemorySize = param.MemorySize,
                DegreeOfParallelism = param.DegreeOfParallelism
            };

            return argon2.GetBytes(param.HashSize);
        }

        public static bool NeedsUpgradeHash(string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;
            return Convert.FromBase64String(storedHash)[0] < CurrentVersion;
        }
    }
}
#endif