using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace CourseWork_Shchegol.Services
{
    public class CryptoService
    {
        public record PublicKey(BigInteger p, BigInteger g, BigInteger y);
        public record PrivateKey(BigInteger p, BigInteger g, BigInteger x);

        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public (PublicKey pub, PrivateKey priv) GenerateKeys(int digits)
        {
            var p = GeneratePrimeWithDigits(digits);
            var g = FindPrimitiveRoot(p);

            var x = RandomInRange(2, p - 2);

            var y = BigInteger.ModPow(g, x, p);

            return (new PublicKey(p, g, y), new PrivateKey(p, g, x));
        }

        public string EncryptBytes(byte[] data, PublicKey pubKey)
        {
            if (pubKey == null)
                throw new ArgumentNullException(nameof(pubKey), "Публічний ключ не був завантажений або дорівнює null.");

            if (pubKey.p == 0 || pubKey.g == 0 || pubKey.y == 0)
                throw new InvalidOperationException("Публічний ключ некоректний (p,g,y не заповнені).");

            var sb = new StringBuilder();

            foreach (var rawByte in data)
            {
                var m = new BigInteger(new byte[] { rawByte, 0x00 });

                if (m <= 0) m = 1;

                var kSession = RandomCoprime(pubKey.p - 1);

                var a = BigInteger.ModPow(pubKey.g, kSession, pubKey.p);

                var s = BigInteger.ModPow(pubKey.y, kSession, pubKey.p);

                var bPart = (m * s) % pubKey.p;

                sb.Append(a).Append(':').Append(bPart).Append('|');
            }

            return sb.ToString();
        }

        public byte[] DecryptToBytes(string cipher, PrivateKey privKey)
        {
            if (privKey == null)
                throw new ArgumentNullException(nameof(privKey), "Приватний ключ не був завантажений або дорівнює null.");

            if (privKey.p == 0 || privKey.g == 0 || privKey.x == 0)
                throw new InvalidOperationException("Приватний ключ некоректний (p,g,x не заповнені).");

            if (string.IsNullOrWhiteSpace(cipher))
                return Array.Empty<byte>();

            var blocks = cipher.Split('|', StringSplitOptions.RemoveEmptyEntries);
            var resultBytes = new System.Collections.Generic.List<byte>();

            foreach (var block in blocks)
            {
                var parts = block.Split(':');
                if (parts.Length != 2)
                    continue; 

                var a = BigInteger.Parse(parts[0]);
                var bPart = BigInteger.Parse(parts[1]);

                var s = BigInteger.ModPow(a, privKey.x, privKey.p);

                var sInv = ModInverse(s, privKey.p);

                var m = (bPart * sInv) % privKey.p;

                var mByte = (byte)(int)m;
                resultBytes.Add(mByte);
            }

            return resultBytes.ToArray();
        }

        public (BigInteger r, BigInteger s) Sign(byte[] data, PrivateKey privKey)
        {
            if (privKey == null)
                throw new ArgumentNullException(nameof(privKey), "Приватний ключ не був завантажений або дорівнює null.");

            var p = privKey.p;
            var g = privKey.g;
            var x = privKey.x;

            var h = HashToBigInt(data) % (p - 1);

            if (h < 0)
                h += (p - 1);

            while (true)
            {
                var kSign = RandomCoprime(p - 1);

                var r = BigInteger.ModPow(g, kSign, p);

                var invK = ModInverse(kSign, p - 1);

                var s = ((h - (x * r)) * invK) % (p - 1);
                if (s < 0) s += (p - 1);

                if (r != 0 && s != 0)
                    return (r, s);
            }
        }
        public bool Verify(byte[] data, (BigInteger r, BigInteger s) sig, PublicKey pubKey)
        {
            if (pubKey == null)
                throw new ArgumentNullException(nameof(pubKey), "Публічний ключ не був завантажений або дорівнює null.");

            var p = pubKey.p;
            var g = pubKey.g;
            var y = pubKey.y;

            if (sig.r <= 0 || sig.r >= p)
                return false;

            var h = HashToBigInt(data) % (p - 1);
            if (h < 0)
                h += (p - 1);

            var rNorm = ((sig.r % p) + p) % p;
            var sNorm = ((sig.s % p) + p) % p;

            var left =
                (BigInteger.ModPow(y, rNorm, p) *
                 BigInteger.ModPow(rNorm, sNorm, p))
                % p;

            var right = BigInteger.ModPow(g, h, p);

            return left == right;
        }

        public void EncryptFile(string inPath, string outPath, PublicKey pubKey)
        {
            if (pubKey == null)
                throw new ArgumentNullException(nameof(pubKey), "Публічний ключ не завантажений.");

            var bytes = File.Exists(inPath) ? File.ReadAllBytes(inPath) : Array.Empty<byte>();
            var cipherText = EncryptBytes(bytes, pubKey);
            FileService.WriteAllText(outPath, cipherText);
        }

        public void DecryptFile(string inPath, string outPath, PrivateKey privKey)
        {
            if (privKey == null)
                throw new ArgumentNullException(nameof(privKey), "Приватний ключ не завантажений.");

            var cipherText = File.Exists(inPath) ? File.ReadAllText(inPath) : string.Empty;
            var plainBytes = DecryptToBytes(cipherText, privKey);
            FileService.WriteAllBytes(outPath, plainBytes);
        }

        public void SignFile(string inPath, string sigPath, PrivateKey privKey)
        {
            if (privKey == null)
                throw new ArgumentNullException(nameof(privKey), "Приватний ключ не завантажений.");

            var bytes = File.Exists(inPath) ? File.ReadAllBytes(inPath) : Array.Empty<byte>();
            var (r, s) = Sign(bytes, privKey);
            FileService.WriteAllText(sigPath, $"{r}|{s}");
        }

        public bool VerifyFile(string inPath, string sigPath, PublicKey pubKey)
        {
            if (pubKey == null)
                throw new ArgumentNullException(nameof(pubKey), "Публічний ключ не завантажений.");

            var bytes = File.Exists(inPath) ? File.ReadAllBytes(inPath) : Array.Empty<byte>();
            var sigText = File.Exists(sigPath) ? File.ReadAllText(sigPath) : string.Empty;

            var parts = sigText.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;

            var r = BigInteger.Parse(parts[0]);
            var s = BigInteger.Parse(parts[1]);

            return Verify(bytes, (r, s), pubKey);
        }

        public static void SavePublic(string path, PublicKey k)
        {
            if (k == null) throw new ArgumentNullException(nameof(k));
            FileService.WriteAllText(path, $"{k.p}\n{k.g}\n{k.y}");
        }

        public static void SavePrivate(string path, PrivateKey k)
        {
            if (k == null) throw new ArgumentNullException(nameof(k));
            FileService.WriteAllText(path, $"{k.p}\n{k.g}\n{k.x}");
        }

        public static PublicKey LoadPublic(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл публічного ключа не знайдено.", path);

            var lines = File
                .ReadAllLines(path)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            if (lines.Length < 3)
                throw new InvalidDataException("Публічний ключ пошкоджений або неповний. Очікується 3 рядки: p, g, y.");

            var p = BigInteger.Parse(lines[0]);
            var g = BigInteger.Parse(lines[1]);
            var y = BigInteger.Parse(lines[2]);

            return new PublicKey(p, g, y);
        }

        public static PrivateKey LoadPrivate(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл приватного ключа не знайдено.", path);

            var lines = File
                .ReadAllLines(path)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            if (lines.Length < 3)
                throw new InvalidDataException("Приватний ключ пошкоджений або неповний. Очікується 3 рядки: p, g, x.");

            var p = BigInteger.Parse(lines[0]);
            var g = BigInteger.Parse(lines[1]);
            var x = BigInteger.Parse(lines[2]);

            return new PrivateKey(p, g, x);
        }

        private static BigInteger HashToBigInt(byte[] data)
        {
            using var sha = SHA256.Create();
            var h = sha.ComputeHash(data);

            return new BigInteger(new byte[] { 0x00 }.Concat(h.Reverse()).ToArray());
        }

        private static BigInteger RandomInRange(BigInteger min, BigInteger max)
        {
            if (max < min) throw new ArgumentException("Границі невірні.");

            var diff = max - min + 1;
            var bytes = diff.ToByteArray();
            BigInteger r;

            do
            {
                Rng.GetBytes(bytes);
                bytes[^1] &= 0x7F; 
                r = new BigInteger(bytes);
            }
            while (r >= diff || r < 0);

            return min + r;
        }

        private static BigInteger RandomCoprime(BigInteger m)
        {
            while (true)
            {
                var k = RandomInRange(2, m - 2);
                if (BigInteger.GreatestCommonDivisor(k, m) == 1)
                    return k;
            }
        }

        private static bool IsProbablePrime(BigInteger n, int rounds = 16)
        {
            if (n < 2) return false;
            if (n % 2 == 0) return n == 2;

            var d = n - 1;
            var s = 0;
            while (d % 2 == 0)
            {
                d /= 2;
                s++;
            }

            for (int i = 0; i < rounds; i++)
            {
                var a = RandomInRange(2, n - 2);
                var x = BigInteger.ModPow(a, d, n);

                if (x == 1 || x == n - 1)
                    continue;

                var witnessFound = true;
                for (int r = 1; r < s; r++)
                {
                    x = BigInteger.ModPow(x, 2, n);
                    if (x == n - 1)
                    {
                        witnessFound = false;
                        break;
                    }
                }
                if (witnessFound)
                    return false;
            }
            return true;
        }

        private static BigInteger GeneratePrimeWithDigits(int digits)
        {
            if (digits < 3)
                throw new ArgumentException("Надто мале p. Вкажіть щось типу 20+ цифр для демонстрації.");

            var min = BigInteger.Pow(10, digits - 1);
            var max = BigInteger.Pow(10, digits) - 1;

            while (true)
            {
                var candidate = RandomInRange(min, max);

                if (candidate % 2 == 0)
                    candidate += 1;

                for (int i = 0; i < 2000; i++, candidate += 2)
                {
                    if (IsProbablePrime(candidate))
                        return candidate;
                }
            }
        }
        private static BigInteger FindPrimitiveRoot(BigInteger p)
        {
            var phi = p - 1;
            var factors = Factorize(phi);

            for (BigInteger g = 2; g < p - 1; g++)
            {
                bool ok = true;
                foreach (var q in factors)
                {
                    if (BigInteger.ModPow(g, phi / q, p) == 1)
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                    return g;
            }

            throw new Exception("Не вдалося знайти первісний корінь для p.");
        }
        private static System.Collections.Generic.List<BigInteger> Factorize(BigInteger n)
        {
            var list = new System.Collections.Generic.List<BigInteger>();

            for (BigInteger d = 2; d * d <= n; d++)
            {
                if (n % d == 0)
                {
                    list.Add(d);
                    while (n % d == 0)
                        n /= d;
                }
            }

            if (n > 1)
                list.Add(n);

            return list;
        }
        private static BigInteger ModInverse(BigInteger a, BigInteger m)
        {
            BigInteger t = 0, newT = 1;
            BigInteger r = m, newR = a % m;

            while (newR != 0)
            {
                var q = r / newR;

                var tmpT = t - q * newT;
                t = newT;
                newT = tmpT;

                var tmpR = r - q * newR;
                r = newR;
                newR = tmpR;
            }

            if (r > 1)
                throw new Exception("Не існує оберненого елемента (a не взаємно просте з m).");

            if (t < 0)
                t += m;

            return t;
        }
    }
}
