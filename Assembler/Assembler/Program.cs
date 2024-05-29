﻿using System.Text.Json;
using System.Text.RegularExpressions;

namespace Assembler
{
    internal class Program
    {
        static bool isLabel(string line, out string? label)
        {
            string pattern = @"^[a-zA-Z_]+:$";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(line);
            if (match.Success)
            {
                label = match.Value.Substring(0, match.Value.Length - 1);
                return true;
            }
            label = default;
            return false;
        }

        static byte ParseByteValue(string literal, Dictionary<string, byte> namedConstants)
        {
            if (literal.Length == 0) throw new AssemblerException();

            if (literal[0] == '[')
            {
                if (literal[^1] != ']') throw new AssemblerException();
                string inner = literal.Substring(1, literal.Length - 2);
                IEnumerable<string> addends = inner.Split('+').Select(x => x.Trim());

                byte sum = 0;
                foreach(string str in addends)
                {
                    sum += ParseByteValue(str, namedConstants);
                }
                return sum;
            }
            else if (namedConstants.ContainsKey(literal))
            {
                return namedConstants[literal];
            }
            else if (literal.Length == 4 && literal[1] == 'x')
            {
                return Convert.ToByte(literal, 16);
            }
            else if (literal.Length == 10 && literal[1] == 'b')
            {
                return Convert.ToByte(literal.Substring(2), 2);
            }
            else
            {
                return (byte)Convert.ToSByte(literal);
            }
        }

        static bool GetCodeLines(IEnumerable<string> lines, out List<string> codeLines, out Dictionary<string, byte> labels, out Dictionary<string, byte> namedConstants)
        {
            codeLines = new List<string>();
            labels = new Dictionary<string, byte>();
            namedConstants = new Dictionary<string, byte>();

            List<string> currentLabels = new();
            foreach (string line in lines)
            {
                string code = line.Split("#")[0].Trim();
                if (code == "") continue;

                bool isNamedConst = code[0] == '.';
                if (isNamedConst)
                {
                    string[] parts = code.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2) throw new AssemblerException();
                    
                    string constName = parts[0].Substring(1);
                    if (constName[0] is >= '0' and <= '9') throw new AssemblerException();

                    byte val = ParseByteValue(parts[1], namedConstants);
                    namedConstants.Add(constName, val);
                }
                else if (isLabel(code, out string label))
                {
                    currentLabels.Add(label);
                }
                else
                {
                    codeLines.Add(code);
                    foreach (string currLabel in currentLabels)
                    {
                        labels.Add(currLabel, (byte)(codeLines.Count - 1));
                    }
                    currentLabels.Clear();
                }
            }
            return true;
        }

        static (byte Opcode, byte Data) AssembleLine(string code, Dictionary<string, byte> labels, Dictionary<string, byte> namedConstants, ISA isa)
        {
            string[] parts = code.Split(' ');
            string opcodeName = parts[0].ToLower();
            Instruction instruction = isa.Instructions.Where(x => x.Name.ToLower() == opcodeName).First();
            string opcodeByteStr = isa.InstructionNumbering[instruction.Name];
            byte opcodeByte = Convert.ToByte(opcodeByteStr, 16);

            ParseType parseType = instruction.ParseType;
            switch (parseType)
            {
                case ParseType.None:
                    if (parts.Length > 1)
                    {
                        throw new AssemblerException();
                    }
                    return (opcodeByte, 0x00);
                case ParseType.Val:
                    string valueStr = code.Substring(opcodeName.Length).Trim();
                    return (opcodeByte, ParseByteValue(valueStr, namedConstants));
                case ParseType.Dest:
                    {
                        if (parts.Length != 2 || parts[1].ToLower()[0] != 'r') throw new AssemblerException();
                        string regName = parts[1];
                        byte regNum = byte.Parse(regName.Substring(1));

                        if (regNum > 4) throw new AssemblerException();
                        return (opcodeByte, (byte)(regNum << 5));   
                    }
                case ParseType.Src:
                    {
                        if (parts.Length != 2 || parts[1].ToLower()[0] != 'r') throw new AssemblerException();
                        string regName = parts[1];
                        byte regNum = byte.Parse(regName.Substring(1));

                        if (regNum > 4) throw new AssemblerException();
                        return (opcodeByte, regNum);
                    }
                case ParseType.DestSrc:
                    {
                        if (parts.Length != 3 || parts[1].ToLower()[0] != 'r' || parts[2].ToLower()[0] != 'r') throw new AssemblerException();
                        byte dest = byte.Parse(parts[1].Substring(1));
                        byte src = byte.Parse(parts[2].Substring(1));

                        if (dest > 4 || src > 4) throw new AssemblerException();
                        return (opcodeByte, (byte)((dest << 5) | src));
                    }
                case ParseType.DestSrcSrc:
                    {
                        if (parts.Length != 4 || parts[1].ToLower()[0] != 'r'
                            || parts[2].ToLower()[0] != 'r' || parts[3].ToLower()[0] != 'r')
                        {
                            throw new AssemblerException();
                        }
                        byte dest = byte.Parse(parts[1].Substring(1));
                        byte src1 = byte.Parse(parts[2].Substring(1));
                        byte src2 = byte.Parse(parts[3].Substring(1));

                        if (dest > 4 || src1 > 3 || src2 > 4) throw new AssemblerException();
                        return (opcodeByte, (byte)((dest << 5) | (src1 << 3) | src2));
                    }
                case ParseType.Label:
                    {
                        if (parts.Length != 2) throw new AssemblerException();
                        string label = parts[1];
                        byte jumpLocation = labels[label];
                        return (opcodeByte, jumpLocation);
                    }
                default:
                    throw new AssemblerException();
            }
        }

        static void Main(string[] args)
        {
            string srcFolder = @"..\..\..\..\..\DemoPrograms\";

            //Get ISA
            string isaPath = srcFolder + "isa.json";
            string isaText = File.ReadAllText(isaPath);
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };
            ISA isa = JsonSerializer.Deserialize<ISA>(isaText, options);

            //Generate ISA microcode
            (byte[] ioMicrocode, byte[] aluMicrocode) = MicrocodeGenerator.Generate(isa);

            string ioMicrocodeOutputFile = srcFolder + "ioMicrocodeROM.bin";
            string aluMicrocodeOutputFile = srcFolder + "aluMicrocodeROM.bin";

            File.WriteAllBytes(ioMicrocodeOutputFile, ioMicrocode);
            File.WriteAllBytes(aluMicrocodeOutputFile, aluMicrocode);

            foreach (string codePath in Directory.EnumerateFiles(srcFolder, "*.lasm"))
            {
                string[] lines = File.ReadAllLines(codePath);
                GetCodeLines(lines, out List<string> codeLines, 
                                    out Dictionary<string, byte> labels, 
                                    out Dictionary<string, byte> namedConstants);
                
                string fileName = Path.GetFileName(codePath);
                Console.WriteLine(fileName + " has " + codeLines.Count() + " instructions");

                //Generate stripped file
                string strippedOutputFile = codePath.Substring(0, codePath.Length - 5) + "_stripped.slasm";
                List<string> strippedFileLines = new(codeLines);
                foreach (string label in labels.Keys)
                {
                    byte index = labels[label];
                    strippedFileLines[index] = label + ": " + strippedFileLines[index];
                }
                File.WriteAllLines(strippedOutputFile, strippedFileLines);

                //Assemble
                (byte Opcode, byte Data)[] assembled = codeLines.Select(x => AssembleLine(x, labels, namedConstants, isa)).ToArray();
                byte[] opcodeBytes = new byte[256];
                byte[] dataBytes = new byte[256];
                Array.Fill<byte>(opcodeBytes, 0x7F);
                assembled.Select(x => x.Opcode).ToArray().CopyTo(opcodeBytes, 0);
                assembled.Select(x => x.Data).ToArray().CopyTo(dataBytes, 0);

                string opcodeOutputFile = codePath.Substring(0, codePath.Length - 5) + "_opcodeROM.bin";
                string dataOutputFile = codePath.Substring(0, codePath.Length - 5) + "_dataROM.bin";

                File.WriteAllBytes(opcodeOutputFile, opcodeBytes);
                File.WriteAllBytes(dataOutputFile, dataBytes);
            }
        }
    }
}
