#if FANTASY_NET
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Konscious.Security.Cryptography;

namespace Fantasy.Product.Authentication
{

    /// Note: 感觉不太可行, 两个原因: 1. PC和手机公平性不足; 2. Argon2这个库的GC压力过大
    /// 
    /// <summary>
    /// 哈希工作量证明帮助类, 常用于要求客户端提供足够工作量才有资格做某事, 
    /// 典型使用场景: 防御海量僵尸客户端的攻击, 可大大提高攻击者电力成本从而逼迫对方放弃。
    /// </summary>
    /// Gemini版
    //public class HashPoW
    //{
    //    // 参数说明 (基于主流移动端/PC中端配置)：

    //    // Iterations: 4 (增加CPU循环)

    //    // MemorySize: 65536 (64MB，关键！1000个僵尸进程就需要64GB内存)

    //    // DegreeOfParallelism: 4 (压榨4个核心)

    //    private const int DefaultMemory = 65536;
    //    private const int DefaultIterations = 4;
    //    private const int DefaultParallelism = 4;

    //    /// <summary>
    //    /// 服务器生成挑战题目
    //    /// </summary>
    //    /// <param name="sessionId">当前连接的唯一ID</param>
    //    /// <param name="clientIp">客户端IP，防止A计算B使用</param>
    //    public static string CreateChallenge(string sessionId, string clientIp)
    //    {
    //        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    //        // 这里的盐值包含了身份信息和时间，黑客无法离线预算

    //        string raw = $"{sessionId}:{clientIp}:{timestamp}";
    //        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    //    }

    //    /// <summary>
    //    /// 客户端执行计算 (异步防止UI卡死)
    //    /// </summary>
    //    public static async Task<byte[]> SolveAsync(string challenge, int memory = DefaultMemory)
    //    {
    //        byte[] salt = Encoding.UTF8.GetBytes(challenge);

    //        // 这里的 password 并不重要，nonce（随机数）我们直接内置在 Argon2 内部处理
    //        // 或者简单起见，我们直接针对 Salt 进行高强度 Hash 迭代
    //        var argon2 = new Argon2id(Encoding.UTF8.GetBytes("PoW_Constant_Pass"));
    //        argon2.Salt = salt;
    //        argon2.DegreeOfParallelism = DefaultParallelism;
    //        argon2.Iterations = DefaultIterations;
    //        argon2.MemorySize = memory;

    //        // 生成 32 字节的结果
    //        return await Task.Run(() => argon2.GetBytes(32));
    //    }

    //    /// <summary>
    //    /// 服务器验证逻辑
    //    /// </summary>
    //    public static bool Verify(string challenge, byte[] clientResult, string currentSessionId, string currentIp)
    //    {
    //        // 1. 基础防重放：解析 challenge

    //        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(challenge));
    //        string[] parts = decoded.Split(':');
    //        if (parts.Length != 3)
    //            return false;

    //        string sid = parts[0];
    //        string ip = parts[1];
    //        long time = long.Parse(parts[2]);

    //        // 2. 校验身份一致性 (严防作弊：必须是同一个人在同一时间段算的)

    //        if (sid != currentSessionId || ip != currentIp)
    //            return false;

    //        // 3. 校验时效性 (例如挑战必须在10分钟内完成)

    //        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - time > 600)
    //            return false;

    //        // 4. 执行一次快速验证 (服务器性能开销远小于客户端，因为服务器通常核心更多)

    //        var argon2 = new Argon2id(Encoding.UTF8.GetBytes("PoW_Constant_Pass"));
    //        argon2.Salt = Encoding.UTF8.GetBytes(challenge);
    //        argon2.DegreeOfParallelism = DefaultParallelism;
    //        argon2.Iterations = DefaultIterations;
    //        argon2.MemorySize = DefaultMemory;

    //        byte[] serverResult = argon2.GetBytes(32);
    //        return serverResult.SequenceEqual(clientResult);
    //    }
    //}

    /// ChatGPT版
    public class HashPoW
    {
        // ================= 配置 =================

        public const int MemoryKB = 192 * 1024; // 192MB
        public const int Iterations = 3;
        public const int Parallelism = 1;

        // 前导0 bit数
        public const int DifficultyBits = 22;

        // ================= Challenge =================

        public class Challenge
        {
            public required string Salt;
            public long Timestamp;
            public required string SessionId;
            public int Difficulty;
        }

        // ================= 服务端 =================

        public static Challenge CreateChallenge(string sessionId)
        {
            return new Challenge
            {
                Salt = RandomHex(16),
                Timestamp = Now(),
                SessionId = sessionId,
                Difficulty = DifficultyBits
            };
        }

        public static bool Verify(
            Challenge c,
            string nonce,
            byte[] hash,
            int expireSeconds = 90)
        {
            if (Now() - c.Timestamp > expireSeconds)
                return false;

            byte[] recompute = ComputeHash(
                c.Salt, c.Timestamp, c.SessionId, nonce);

            if (!FixedTimeEquals(hash, recompute))
                return false;

            return CheckDifficulty(hash, c.Difficulty);
        }

        // ================= 客户端 =================

        /// <summary>
        /// 后台启动PoW(用户操作期间跑)
        /// </summary>
        public static Task<(string nonce, byte[] hash)> StartAsync(
            Challenge c,
            CancellationToken token = default)
        {
            return Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    string nonce = RandomHex(8);
                    byte[] hash = ComputeHash(
                        c.Salt, c.Timestamp, c.SessionId, nonce);

                    if (CheckDifficulty(hash, c.Difficulty))
                        return (nonce, hash);
                }

                throw new OperationCanceledException();
            });
        }

        // ================= 核心计算 =================

        private static byte[] ComputeHash(
            string salt,
            long ts,
            string session,
            string nonce)
        {
            byte[] input = Encoding.UTF8.GetBytes(
                $"{salt}:{ts}:{session}:{nonce}");

            var argon2 = new Argon2id(input)
            {
                Salt = Encoding.UTF8.GetBytes(salt),
                MemorySize = MemoryKB,
                Iterations = Iterations,
                DegreeOfParallelism = Parallelism
            };

            return argon2.GetBytes(32);
        }

        // ================= 工具 =================

        private static bool CheckDifficulty(byte[] hash, int bits)
        {
            int count = 0;

            foreach (byte b in hash)
            {
                for (int i = 7; i >= 0; i--)
                {
                    if ((b & (1 << i)) == 0)
                        count++;
                    else
                        return count >= bits;
                }
            }
            return true;
        }

        private static string RandomHex(int bytes)
        {
            return Convert.ToHexString(
                RandomNumberGenerator.GetBytes(bytes));
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static long Now()
            => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
#endif