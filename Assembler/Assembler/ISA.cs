using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Assembler
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ParseType
    { 
        None,
        Val,
        Dest,
        Src,
        DestSrc,
        DestSrcSrc,
        Label,
    }

    public struct MicrocodeStep
    {
        public string Read { get; set; }
        public string Write { get; set; }
        public bool? RAMAddrFromData { get; set; }
        public bool? R4B { get; set; }
        public string? ALUOp { get; set; }
    }

    public class Instruction
    {
        public string Name { get; set; }
        public ParseType ParseType { get; set; }
        public MicrocodeStep[] Microcode { get; set; }
    }

    public class ISA
    {
        public Dictionary<string, string> InstructionNumbering { get; set; }
        public Dictionary<string, byte> ReadIndices { get; set; }
        public Dictionary<string, byte> WriteIndices { get; set; }
        public Dictionary<string, string> ALUOperations{ get; set; }
        public Instruction[] Instructions { get; set; }

    }
}
