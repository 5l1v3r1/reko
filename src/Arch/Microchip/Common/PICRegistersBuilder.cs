﻿#region License
/* 
 * Copyright (C) 2017-2018 Christian Hostelet.
 * inspired by work from:
 * Copyright (C) 1999-2018 John Källén.
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

using Reko.Libraries.Microchip;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Reko.Arch.MicrochipPIC.Common
{

    /// <summary>
    /// A PIC registers symbol table builder. Must be inherited.
    /// </summary>
    public abstract class PICRegistersBuilder : IMemDataRegionVisitor, IMemDataSymbolVisitor
    {
        private int byteAddr = 0;
        private ulong bitRegAddr = 0;
        private static int regNumber = 0;
//        private ISFRRegister currSFRDef = null;
//        private PICRegisterStorage currSFRReg = null;
        private IPICRegisterSymTable symTable;

        /// <summary>
        /// Loads the PIC registers, as found in the PIC definition, into the registers symbol table.
        /// </summary>
        /// <param name="registersSymTable">The registers symbol table interface.</param>
        /// <param name="pic">the PIC definition.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="registersSymTable"/> is
        ///                                         null.</exception>
        protected void LoadRegistersInTable(IPICRegisterSymTable registersSymTable, IPICDescriptor pic)
        {
            if (pic == null)
                throw new ArgumentNullException(nameof(pic));
            symTable = registersSymTable ?? throw new ArgumentNullException(nameof(registersSymTable));
            regNumber = 0;

            foreach (var sfr in pic.PICRegisters.SFRs)
            {
                var reg = AddSFRRegister(sfr, regNumber);
                regNumber++;
                symTable.AddRegister(reg);
            }
            foreach (var jsfr in pic.PICRegisters.JoinedRegisters)
            {
                var subregs = new List<PICRegisterStorage>();
                bitRegAddr = 0;
                foreach (var sfr in jsfr.ChildSFRs)
                {
                    if (!PICRegisters.TryGetRegister(sfr.Name, out var creg))
                    {
                        creg = AddSFRRegister(sfr, regNumber++);
                    }
                    creg.BitAddress = bitRegAddr;
                    subregs.Add(creg);
                    bitRegAddr += 8;
                }
                if (subregs.Count > 0)
                {
                    var sfr = new PICRegisterStorage(jsfr, regNumber, subregs);
                    if (symTable.AddRegister(sfr))
                        regNumber++;
                }
            }
        }


        private PICRegisterStorage AddSFRRegister(ISFRRegister sfr, int regnum)
        {
            var reg = new PICRegisterStorage(sfr, regNumber) { BitAddress = 0UL };
            foreach (var sfld in sfr.BitFields)
            {
                if ((sfld.Name != sfr.Name) && (sfld.BitWidth != sfr.BitWidth))
                {
                    var fld = new PICRegisterBitFieldStorage(reg, sfld);
                    if (!reg.BitFields.ContainsKey(fld.BitFieldSortKey))
                    {
                        if (symTable.AddRegisterBitField(fld))
                        {
                            reg.BitFields.Add(fld.BitFieldSortKey, fld);
                            continue;
                        }
                    }
                }
            }
            return reg;
        }

        #region Visit the PIC definition and create registers

        public void Visit(SFRDataSector xmlRegion)
        {
            foreach (var sfr in xmlRegion.SFRs)
            {
                bitRegAddr = 0;
                sfr.Accept(this);
            }
            foreach (var join in xmlRegion.JoinedSFRs)
            {
                bitRegAddr = 0;
                join.Accept(this);
            }
            foreach (var mux in xmlRegion.MuxedSFRs)
            {
                bitRegAddr = 0;
                mux.Accept(this);
            }
        }

        public void Visit(GPRDataSector xmlRegion)
        {
            // Do nothing        
        }

        public void Visit(DPRDataSector xmlRegion)
        {
            // Do nothing        
        }

        public void Visit(NMMRPlace xmlRegion)
        {
            if (xmlRegion.RegionID != "peripheralnmmrs")  // Do not consider internal peripherals (for hardware debuggers only).
            {
                xmlRegion.SFRDefs.ForEach(e => e.Accept(this));
            }
        }

        public void Visit(LinearDataSector xmlRegion)
        {
            // Do nothing        
        }


        public void Visit(DataBitAdjustPoint xmlSymb)
        {
        }

        public void Visit(DataByteAdjustPoint xmlSymb)
        {
            byteAddr += xmlSymb.Offset;
        }

        public void Visit(JoinedSFRDef xmlSymb)
        {
//            var subregs = new List<PICRegisterStorage>();
//            bitRegAddr = 0;
//            xmlSymb.SFRs.ForEach(e =>
//            {
//                e.Accept(this);
//                currSFRReg.BitAddress = bitRegAddr;
//                bitRegAddr += (ulong)currSFRDef.BitWidth;
//                subregs.Add(currSFRReg);
//                regNumber--;
//            });
//            if (subregs.Count > 0)
//            {
//                var sfr = new PICRegisterStorage(xmlSymb, regNumber, subregs);
//                if (symTable.AddRegister(sfr))
//                    regNumber++;
//            }
        }

        public void Visit(MuxedSFRDef xmlSymb)
        {
            foreach (var selsfr in xmlSymb.SelectSFRs)
            {
                if (String.IsNullOrEmpty(selsfr.When))
                {
                    var sfr = selsfr.SFR;
                    sfr.Accept(this);
                    return;
                }
                selsfr.Accept(this);
                regNumber++;
            }
        }

        public void Visit(SelectSFR xmlSymb)
        {
            xmlSymb.SFR.Accept(this);
        }

        public void Visit(SFRDef xmlSymb)
        {
//            currSFRReg = new PICRegisterStorage(xmlSymb, regNumber) { BitAddress = 0UL };
//            currSFRDef = xmlSymb;
//            xmlSymb.SFRModes.ForEach(e => e.Accept(this));
//            regNumber++;
//            symTable.AddRegister(currSFRReg);
        }

        public void Visit(SFRModeList xmlSymb)
        {
            xmlSymb.SFRModes.ForEach(e => e.Accept(this));
        }

        public void Visit(SFRMode xmlSymb)
        {
            foreach (var e in xmlSymb.Fields)
            {
                switch (e)
                {
                    case DataBitAdjustPoint adj:
                        adj.Accept(this);
                        continue;

                    case SFRFieldDef sfrfield:
                        sfrfield.Accept(this);
                        continue;
                }

            }
        }

        public void Visit(SFRFieldDef xmlSymb)
        {
//            if (xmlSymb is null)
//                throw new ArgumentNullException(nameof(xmlSymb));
//            if (currSFRDef is null)
//                throw new ArgumentException();
//            // We do not add SFR Fields which are duplicating the parent SFR register definition (same name or same bit width or same position)
//            if ((xmlSymb.Name != currSFRDef.Name) && (xmlSymb.BitWidth != currSFRDef.BitWidth))
//            {
//                var fld = new PICRegisterBitFieldStorage(currSFRReg, xmlSymb);
//                if (!currSFRReg.BitFields.ContainsKey(fld.BitFieldSortKey))
//                {
//                    if (symTable.AddRegisterBitField(fld))
//                    {
//                        currSFRReg.BitFields.Add(fld.BitFieldSortKey, fld);
//                        return;
//                    }
//                }
//                fld = null;
//            }
        }

        public void Visit(SFRFieldSemantic xmlSymb)
        {
            //TODO? SFR Field semantics
        }

        public void Visit(Mirror xmlSymb)
        {
            // Do nothing
        }

        public void Visit(DMARegisterMirror xmlSymb)
        {
            // Do nothing
        }

        #endregion

    }

}
