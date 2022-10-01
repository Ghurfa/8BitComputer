namespace FizzBuzzNoIfs;

unsafe class Program
{
    static ConsoleColor[] Colors =
    {
        ConsoleColor.Gray,
        ConsoleColor.Green,
        ConsoleColor.Red,
        ConsoleColor.Blue
    };

    static char[][] Outputs =
    {
        new char[2],
        new char[] { 'B', 'u', 'z', 'z'},
        new char[] { 'F', 'i', 'z', 'z'},
        new char[] { 'F', 'i', 'z', 'z', 'B', 'u', 'z', 'z'},
    };

    static void FizzBuzz(int value)
    {
        //Convert value to decimal
        int valueCopy = value;
        Outputs[0][1] = (char)('0' + valueCopy % 10);
        valueCopy /= 10;
        Outputs[0][0] = (char)('0' + valueCopy % 10);

        //Select output
        int minus1 = value - 1;
        int divBy3 = (minus1 % 3) & 2;
        int divBy5 = ((minus1 % 5) >> 2) & 1;
        int index = divBy3 | divBy5;

        //Print
        Console.ForegroundColor = Colors[index];
        Console.WriteLine(Outputs[index]);
    }

    //static char[] Outputs2 =
    //{
    //    '\0', '\0',
    //    'F', 'i', 'z', 'z',
    //    'B', 'u', 'z', 'z',
    //    '\0', '\0', '\0', '\0',
    //};

    //static int If(int condition, int ifTrue, int ifFalse)
    //{
    //    int ret = condition * ifTrue + (1 - condition) * ifFalse;
    //    return ret;
    //}

    //static void FizzBuzz2(int value)
    //{
    //    //Convert value to decimal
    //    int valueCopy = value;
    //    Outputs2[1] = (char)('0' + valueCopy % 10);
    //    valueCopy /= 10;
    //    Outputs2[0] = (char)('0' + valueCopy % 10);

    //    //Calc start
    //    int isOneDigit = (int)((((uint)value - 10) & 0x40) >> 6);
    //    int minus1 = value - 1;
    //    int divBy3 = ((minus1 % 3) >> 1) & 1;
    //    int divBy5 = ((minus1 % 5) >> 2) & 1;
    //    int start = ((1 - divBy3) * (1 - divBy5) * isOneDigit) +
    //                (2 * divBy3) +
    //                ((1 - divBy3) * divBy5 * 6);

    //    //Calc len
    //    int len = ((1 - divBy3) * (1 - divBy5) * (2 - isOneDigit)) +
    //              (4 * ((divBy3 ^ divBy5) + (divBy3 & divBy5)) - 1);

    //    Console.Write((char)If(((1 - len--) & 0x100) >> 8, Outputs2[start++], ' '));
    //    Console.Write((char)If(((1 - len--) & 0x100) >> 8, Outputs2[start++], ' '));
    //    Console.Write((char)If(((1 - len--) & 0x100) >> 8, Outputs2[start++], ' '));
    //    Console.Write((char)If(((1 - len--) & 0x100) >> 8, Outputs2[start++], ' '));
    //    Console.Write((char)If(((1 - len--) & 0x100) >> 8, Outputs2[start++], ' '));
    //    Console.Write((char)If(((1 - len--) & 0x100) >> 8, Outputs2[start++], ' '));
    //    Console.Write((char)If(((1 - len--) & 0x100) >> 8, Outputs2[start++], ' '));
    //    Console.WriteLine((char)If(((1 - len--) & 0x100) >> 8, Outputs2[start++], ' '));

    //}

    public static void Main(string[] args)
    {
        for (int i = 0; i < 50; i++)
        {
            FizzBuzz(i);
        }
    }
}
