using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Assembler
{
    internal static class MicrocodeGenerator
    {
        public static (byte[] IOMicroCode, byte[] ALUMicrocode) Generate(ISA isa)
        {
            VerifyISA(isa);
            byte[] ioMicrocode = new byte[16 * 128];
            byte[] aluMicrocode = new byte[16 * 128];

            byte dontWrite = (byte)(isa.WriteIndices["none"] << 4 | isa.ReadIndices["none"]);
            Array.Fill(ioMicrocode, dontWrite);

            foreach(Instruction inst in isa.Instructions)
            {
                byte opcodeByte = Convert.ToByte(isa.InstructionNumbering[inst.Name], 16);
                for(int i = 0; i < inst.Microcode.Length; i++)
                {
                    MicrocodeStep step = inst.Microcode[i];
                    
                    int readIndex = isa.ReadIndices[step.Read];
                    int writeIndex = isa.WriteIndices[step.Write];
                    ioMicrocode[opcodeByte * 16 + i] = (byte)(writeIndex << 4 | readIndex);

                    bool notDone = i < (inst.Microcode.Length - 1);
                    bool ramAddrFromData = step.RAMAddrFromData ?? false;
                    bool dontOutputReg4ToBBus = !step.R4B ?? false;
                    byte ALUOperationBits;
                    if(step.ALUOp != null)
                    {
                        ALUOperationBits = Convert.ToByte(isa.ALUOperations[step.ALUOp].Substring(2), 2);
                    }
                    else
                    {
                        ALUOperationBits = Convert.ToByte(isa.ALUOperations["==ff"].Substring(2), 2);
                    }

                    byte notDoneBit = (byte)((notDone ? 1 : 0) << 7);
                    byte rafdBit = (byte)((ramAddrFromData ? 1 : 0) << 6);
                    byte nR4BBit = (byte)((dontOutputReg4ToBBus ? 1 : 0) << 5);
                    aluMicrocode[opcodeByte * 16 + i] = (byte)(notDoneBit | rafdBit | nR4BBit | ALUOperationBits);
                }
            }

            return (ioMicrocode, aluMicrocode);
        }

        private static void VerifyISA(ISA isa)
        {
            //If these hardcoded names do not exist, this func needs to be updated
            string[] specialWriteNames = { "rx", "ram" };
            string[] specialReadNames = { "rx", "ram", "data" };
            if (specialWriteNames.Where(x => !isa.WriteIndices.ContainsKey(x)).Any() ||
                specialReadNames.Where(x => !isa.ReadIndices.ContainsKey(x)).Any())
            {
                throw new AssemblerException();
            }

            //Read & write indices must be less than 16
            foreach (int index in isa.ReadIndices.Values)
            {
                if (index > 15) throw new AssemblerException();
            }
            foreach (int index in isa.WriteIndices.Values)
            {
                if (index > 15) throw new AssemblerException();
            }

            HashSet<string> usedALUOpNames = new();
            HashSet<byte> usedALUOpBytes = new();
            foreach(string opName in isa.ALUOperations.Keys)
            {
                //ALU operations must have unique names and byte codes
                byte byteCode = Convert.ToByte(isa.ALUOperations[opName].Substring(2), 2);
                if (!usedALUOpNames.Add(opName) || !usedALUOpBytes.Add(byteCode)) throw new AssemblerException();

                //Byte codes must be 5 bits
                if (byteCode > 0b11111) throw new AssemblerException();
            }

            HashSet<string> usedInstNums = new();
            HashSet<string> usedInstNames = new();
            foreach (Instruction inst in isa.Instructions)
            {
                //Opcode byte must be specified and less than 128
                if(!isa.InstructionNumbering.TryGetValue(inst.Name, out string byteStr) || Convert.ToByte(byteStr, 16) > 0x7F)
                {
                    throw new AssemblerException();
                }

                //Name & byte code must be unique
                if (!usedInstNames.Add(inst.Name) ||
                    !usedInstNums.Add(byteStr))
                {
                    throw new AssemblerException();
                }

                //Reading/writing to data-dependent register must only happen on the correct parse type
                bool readsRx = inst.Microcode.Where(x => x.Read == "rx").Any();
                bool writesRx = inst.Microcode.Where(x => x.Write == "rx").Any();
                switch (inst.ParseType)
                {
                    case ParseType.None:
                    case ParseType.Val:
                    case ParseType.Label:
                        if (readsRx || writesRx)
                        {
                            throw new AssemblerException();
                        }
                        break;
                    case ParseType.Dest:
                        if (!writesRx || readsRx)
                        {
                            throw new AssemblerException();
                        }
                        break;
                    case ParseType.Src:
                        if (writesRx || !readsRx)
                        {
                            throw new AssemblerException();
                        }
                        break;
                    case ParseType.DestSrc:
                        if (!writesRx || !readsRx)
                        {
                            throw new AssemblerException();
                        }
                        break;
                    case ParseType.DestSrcSrc:
                        if (!writesRx || !readsRx)
                        {
                            throw new AssemblerException();
                        }
                        break;
                }

                foreach (MicrocodeStep mcStep in inst.Microcode)
                {
                    //If writing to a register, then ALU op must be specified
                    if (isa.WriteIndices[mcStep.Write] < 8 && mcStep.ALUOp == null)
                    {
                        throw new AssemblerException();
                    }

                    //ALU op must be in the ALU op list
                    if (mcStep.ALUOp != null && !isa.ALUOperations.ContainsKey(mcStep.ALUOp))
                    {
                        throw new AssemblerException();
                    }

                    //If reading or writing to RAM, RAMAddrFromData must be specified
                    if ((mcStep.Read == "ram" || mcStep.Write == "ram") && mcStep.RAMAddrFromData == null)
                    {
                        throw new AssemblerException();
                    }

                    //If reading from data and writing to ram, RAMAddrFromData must be false
                    if (mcStep.Read == "data" && mcStep.Write == "ram" && (mcStep.RAMAddrFromData ?? true))
                    {
                        throw new AssemblerException();
                    }

                    //Read and write must be specified and in the read and write indices list
                    if (mcStep.Read == null || mcStep.Write == null || !isa.ReadIndices.ContainsKey(mcStep.Read) || !isa.WriteIndices.ContainsKey(mcStep.Write))
                    {
                        throw new AssemblerException();
                    }
                }
            }
        }
    }
}
