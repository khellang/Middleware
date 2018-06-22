using System.Net;

namespace Hellang.Middleware.RateLimiting
{
    public class IPNetwork
    {
        public IPNetwork(IPAddress prefix, int prefixLength)
        {
            Prefix = prefix;
            PrefixLength = prefixLength;
            PrefixBytes = prefix.GetAddressBytes();
            Mask = CreateMask(PrefixBytes, PrefixLength);
        }

        public IPAddress Prefix { get; }

        public int PrefixLength { get; }

        private byte[] PrefixBytes { get; }

        private byte[] Mask { get; }

        public bool Contains(IPAddress address)
        {
            if (address.AddressFamily != Prefix.AddressFamily)
            {
                return false;
            }

            var addressBytes = address.GetAddressBytes();

            for (int i = 0; i < PrefixBytes.Length && Mask[i] != 0; i++)
            {
                if (PrefixBytes[i] != (addressBytes[i] & Mask[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static byte[] CreateMask(byte[] prefix, int length)
        {
            var i = 0;
            var remainingBits = length;
            var mask = new byte[prefix.Length];

            while (remainingBits >= 8)
            {
                mask[i++] = 0xFF;
                remainingBits -= 8;
            }

            if (remainingBits > 0)
            {
                mask[i] = (byte) (0xFF << (8 - remainingBits));
            }

            return mask;
        }
    }
}
