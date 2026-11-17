using System;
using System.IO;
using System.Security.Cryptography;
using JftwGo.Core.Models;

namespace JftwGo.Core.Verify
{
    /// <summary>文件完整性校验：按 ChecksumInfo 指定算法计算并比对。</summary>
    public static class ChecksumVerifier
    {
        public class VerifyResult
        {
            public bool WasRequested { get; set; }
            public bool Success { get; set; }
            public string Expected { get; set; }
            public string Actual { get; set; }
            public string ErrorMessage { get; set; }
        }

        public static VerifyResult Verify(string filePath, ChecksumInfo checksum)
        {
            var result = new VerifyResult();
            if (checksum == null || string.IsNullOrWhiteSpace(checksum.Value) || !checksum.IsSupported)
            {
                result.WasRequested = false;
                result.Success = true; // 无校验信息时不阻断，但标记为未请求
                return result;
            }

            result.WasRequested = true;
            result.Expected = checksum.Value.Trim();
            try
            {
                string algo = checksum.Algorithm.Trim().ToUpperInvariant().Replace("-", "");
                using (HashAlgorithm hash = CreateHash(algo))
                {
                    if (hash == null)
                    {
                        result.ErrorMessage = "不支持的校验算法：" + checksum.Algorithm;
                        return result;
                    }
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        byte[] computed = hash.ComputeHash(fs);
                        result.Actual = ToHex(computed);
                    }
                }
                result.Success = string.Equals(result.Expected, result.Actual,
                    StringComparison.OrdinalIgnoreCase);
                if (!result.Success)
                    result.ErrorMessage = "校验和不匹配：期望 " + result.Expected + "，实际 " + result.Actual;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "校验失败：" + ex.Message;
            }
            return result;
        }

        private static HashAlgorithm CreateHash(string algo)
        {
            switch (algo)
            {
                case "SHA256": return SHA256.Create();
                case "SHA1": return SHA1.Create();
                case "MD5": return MD5.Create();
                default: return null;
            }
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new System.Text.StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
