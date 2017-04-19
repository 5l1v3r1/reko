#region License
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

using Reko.Core.Serialization;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Reko.Core.Configuration
{
    public interface OperatingEnvironment
    {
        string Name { get; }
        string Description { get; }
        string TypeName { get; }
        string MemoryMapFile { get; }
        Dictionary<string, object> Options { get; }

        PlatformHeuristics_v1 Heuristics { get; }
        List<ITypeLibraryElement> TypeLibraries { get; }
        List<ITypeLibraryElement> CharacteristicsLibraries { get; }
        List<SignatureFile> SignatureFiles { get; }
        List<IPlatformArchitectureElement> Architectures { get; }

        IPlatform Load(IServiceProvider services, IProcessorArchitecture arch);
    }

    public class OperatingEnvironmentElement : OperatingEnvironment
    {
        public OperatingEnvironmentElement()
        {
            this.TypeLibraries = new List<ITypeLibraryElement>();
            this.CharacteristicsLibraries = new List<ITypeLibraryElement>();
            this.SignatureFiles = new List<SignatureFile>();
            this.Architectures = new List<IPlatformArchitectureElement>();
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public PlatformHeuristics_v1 Heuristics { get; set; }

        public string TypeName { get; set; }

        public string MemoryMapFile { get; set; }

        public List<ITypeLibraryElement> TypeLibraries { get; internal set; }
        public List<ITypeLibraryElement> CharacteristicsLibraries { get; internal set; }
        public List<IPlatformArchitectureElement> Architectures { get; internal set; }
        public List<SignatureFile> SignatureFiles { get; internal set; }
        public Dictionary<string, object> Options { get; internal set; }

        public IPlatform Load(IServiceProvider services, IProcessorArchitecture arch)
        {
            var type = Type.GetType(TypeName);
            if (type == null)
                throw new TypeLoadException(
                    string.Format("Unable to load {0} environment.", Description));
            var platform = (Platform)Activator.CreateInstance(type, services, arch);
            LoadSettingsFromConfiguration(services, platform);
            return platform;
        }

        public void LoadSettingsFromConfiguration(IServiceProvider services, Platform platform)
        {
            platform.Name = this.Name;
            if (!string.IsNullOrEmpty(MemoryMapFile))
            {
                platform.MemoryMap = MemoryMap_v1.LoadMemoryMapFromFile(services, MemoryMapFile, platform);
            }
            platform.Description = this.Description;
            platform.Heuristics = LoadHeuristics(this.Heuristics);
        }

        private PlatformHeuristics LoadHeuristics(PlatformHeuristics_v1 heuristics)
        {
            if (heuristics == null)
            {
                return new PlatformHeuristics
                {
                    ProcedurePrologs = new BytePattern[0],
                };
            }
            BytePattern[] prologs;
            if (heuristics.ProcedurePrologs == null)
            {
                prologs = new BytePattern[0];
            }
            else
            {
                prologs = heuristics.ProcedurePrologs
                    .Select(p => LoadBytePattern(p))
                    .Where(p => p.Bytes != null)
                    .ToArray();
            }

            return new PlatformHeuristics
            {
                ProcedurePrologs = prologs
            };
        }

        public BytePattern LoadBytePattern(BytePattern_v1 sPattern)
        {
            List<byte> bytes = null;
            List<byte> mask = null;
            if (sPattern.Bytes == null)
                return null;
            if (sPattern.Mask == null)
            {
                bytes = new List<byte>();
                mask = new List<byte>();
                int shift = 4;
                int bb = 0;
                int mm = 0;
                for (int i = 0; i < sPattern.Bytes.Length; ++i)
                {
                    char c = sPattern.Bytes[i];
                    byte b;
                    if (BytePattern.TryParseHexDigit(c, out b))
                    {
                        bb = bb | (b << shift);
                        mm = mm | (0x0F << shift);
                        shift -= 4;
                        if (shift < 0)
                        {
                            bytes.Add((byte)bb);
                            mask.Add((byte)mm);
                            shift = 4;
                            bb = mm = 0;
                        }
                    }
                    else if (c == '?' || c == '.')
                    {
                        shift -= 4;
                        if (shift < 0)
                        {
                            bytes.Add((byte)bb);
                            mask.Add((byte)mm);
                            shift = 4;
                            bb = mm = 0;
                        }
                    }
                }
                Debug.Assert(bytes.Count == mask.Count);
            }
            else
            {
                bytes = LoadHexPattern(sPattern.Bytes);
                mask = LoadHexPattern(sPattern.Mask);
            }
            if (bytes.Count == 0)
                return null;
            else
                return new BytePattern
                {
                    Bytes = bytes.ToArray(),
                    Mask = mask.ToArray()
                };

        }

        private List<byte> LoadHexPattern(string sBytes)
        {
            int shift = 4;
            int bb = 0;
            var bytes = new List<byte>();
            for (int i = 0; i < sBytes.Length; ++i)
            {
                char c = sBytes[i];
                byte b;
                if (BytePattern.TryParseHexDigit(c, out b))
                {
                    bb = bb | (b << shift);
                    shift -= 4;
                    if (shift < 0)
                    {
                        bytes.Add((byte)bb);
                        shift = 4;
                        bb = 0;
                    }
                }
            }
            return bytes;
        }

        public override string ToString()
        {
            return Description;
        }
    }
}
