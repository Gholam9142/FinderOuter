﻿// The FinderOuter
// Copyright (c) 2020 Coding Enthusiast
// Distributed under the MIT software license, see the accompanying
// file LICENCE or http://www.opensource.org/licenses/mit-license.php.

using Autarkysoft.Bitcoin.Cryptography.Asymmetric.EllipticCurve;
using Autarkysoft.Bitcoin.Cryptography.EllipticCurve;
using FinderOuter.Services.Comparers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests.Services.Comparers
{
    public class PrvToAddrBothComparerTests
    {
        public static IEnumerable<object[]> GetHashCases()
        {
            yield return new object[] { KeyHelper.Pub1CompAddr, true };
            yield return new object[] { KeyHelper.Pub1CompAddr + "1", false };
            yield return new object[] { KeyHelper.Pub1NestedSegwit, false };
            yield return new object[] { KeyHelper.Pub1NestedSegwit + "1", false };
            yield return new object[] { KeyHelper.Pub1BechAddr, true };
            yield return new object[] { KeyHelper.Pub1BechAddr + "a", false };

            yield return new object[] { "bc1qrp33g0q5c5txsp9arysrx4k6zdkfs4nce4xj0gdcccefvpysxf3qccfmv3", false };
        }

        [Theory]
        [MemberData(nameof(GetHashCases))]
        public void InitTest(string addr, bool expected)
        {
            PrvToAddrBothComparer comp = new();
            bool actual = comp.Init(addr);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CloneTest()
        {
            PrvToAddrBothComparer original = new();
            Assert.True(original.Init(KeyHelper.Pub1CompAddr)); // Make sure it is successfully initialized
            ICompareService cloned = original.Clone();
            // Change original field value to make sure it is cloned not a reference copy
            Assert.True(original.Init(KeyHelper.Pub2CompAddr));

            byte[] key = KeyHelper.Prv1.ToBytes();

            // Since the original was changed it should fail when comparing
            Assert.False(original.Compare(key));
            Assert.True(cloned.Compare(key));
        }

        [Fact]
        public void Compare_CompressedTest()
        {
            PrvToAddrBothComparer comp = new();
            Assert.True(comp.Init(KeyHelper.Pub1CompAddr));
            byte[] key = KeyHelper.Prv1.ToBytes();
            key[0]++;

            bool b = comp.Compare(key);
            Assert.False(b);

            key[0]--;
            b = comp.Compare(key);
            Assert.True(b);
        }

        [Fact]
        public void Compare_UncompressedTest()
        {
            PrvToAddrBothComparer comp = new();
            Assert.True(comp.Init(KeyHelper.Pub1UnCompAddr));
            byte[] key = KeyHelper.Prv1.ToBytes();
            key[0]++;

            bool b = comp.Compare(key);
            Assert.False(b);

            key[0]--;
            b = comp.Compare(key);
            Assert.True(b);
        }

        [Fact]
        public void Compare_EdgeTest()
        {
            PrvToAddrBothComparer comp = new();
            Assert.True(comp.Init(KeyHelper.Pub1CompAddr));
            byte[] key = new byte[32];
            bool b = comp.Compare(key);
            Assert.False(b);

            ((Span<byte>)key).Fill(255);
            b = comp.Compare(key);
            Assert.False(b);

            key = new SecP256k1().N.ToByteArray(true, true);
            b = comp.Compare(key);
            Assert.False(b);
        }

        public static IEnumerable<object[]> GetCases()
        {
            PrvToAddrBothComparer comp = new();
            PrvToAddrBothComparer uncomp = new();
            Assert.True(comp.Init(KeyHelper.Pub1CompAddr));
            Assert.True(uncomp.Init(KeyHelper.Pub1UnCompAddr));

            yield return new object[] { comp, new byte[32], false };
            yield return new object[] { comp, Enumerable.Repeat((byte)255, 32).ToArray(), false };
            yield return new object[] { comp, KeyHelper.Prv1.ToBytes(), true };
            yield return new object[] { comp, KeyHelper.Prv2.ToBytes(), false };

            yield return new object[] { uncomp, new byte[32], false };
            yield return new object[] { uncomp, Enumerable.Repeat((byte)255, 32).ToArray(), false };
            yield return new object[] { uncomp, KeyHelper.Prv1.ToBytes(), true };
            yield return new object[] { uncomp, KeyHelper.Prv2.ToBytes(), false };
        }

        [Theory]
        [MemberData(nameof(GetCases))]
        public unsafe void Compare_Sha256Hpt_Test(PrvToAddrBothComparer comp, byte[] key, bool expected)
        {
            uint* hPt = stackalloc uint[8];
            Helper.WriteToHpt(key, hPt);
            bool actual = comp.Compare(hPt);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetCases))]
        public unsafe void Compare_Sha512Hpt_Test(PrvToAddrBothComparer comp, byte[] key, bool expected)
        {
            ulong* hPt = stackalloc ulong[8];
            Helper.WriteToHpt32(key, hPt);
            bool actual = comp.Compare(hPt);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(GetCases))]
        public unsafe void Compare_PointJ_Test(PrvToAddrBothComparer comp, byte[] key, bool expected)
        {
            Scalar8x32 sc = new(key, out bool overflow);
            if (!overflow && !sc.IsZero)
            {
                PointJacobian point = Helper.Calc.MultiplyByG(sc);
                bool actual = comp.Compare(point);
                Assert.Equal(expected, actual);
            }
        }
    }
}
