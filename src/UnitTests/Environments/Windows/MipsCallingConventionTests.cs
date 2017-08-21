﻿#region License
/* 
 * Copyright (C) 1999-2017 John Källén.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#endregion

using NUnit.Framework;
using Reko.Arch.Mips;
using Reko.Core.Serialization;
using Reko.Core.Types;
using Reko.Environments.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reko.Core;
using Rhino.Mocks;

namespace Reko.UnitTests.Environments.Windows
{
    [TestFixture]
    public class MipsCallingConventionTests
    {
        private PrimitiveType i32 = PrimitiveType.Int32;
        private VoidType v = VoidType.Instance;
        private MockRepository mr;
        private MipsLe32Architecture arch;
        private Reko.Environments.Windows.MipsCallingConvention cc;

        [SetUp]
        public void Setup()
        {
            this.mr = new MockRepository();
            this.arch = new MipsLe32Architecture();
        }

        private Pointer Ptr(DataType dt)
        {
            return new Pointer(dt, 4);
        }

        private void Given_CallingConvention()
        {
            var cc = new MipsCallingConvention(arch);
            this.cc = cc;
        }

        [Test]
        public void MipsProcSer_ReturnRegister()
        {
            Given_CallingConvention();
            var ccr = cc.Generate(i32, null, new List<DataType> { });
            Assert.AreEqual("r2", ccr.ToString());
        }

        [Test]
        public void MipsProcSet_CharArg()
        {
            Given_CallingConvention();
            var ccr = cc.Generate(null, null,new List<DataType> { Ptr(v) });
            Assert.AreEqual("r4", ccr.ToString());
        }

        [Test]
        public void MipsProcSet_ManyArgs()
        {
            Given_CallingConvention();
            var ccr = cc.Generate(null, null, new List<DataType> { Ptr(v), Ptr(v), Ptr(v), Ptr(v), Ptr(v) });
            Assert.AreEqual("@@@", ccr.ToString());
        }
    }
}
