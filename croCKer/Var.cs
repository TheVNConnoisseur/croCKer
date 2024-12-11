using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace croCKer
{
    internal class Var
    {
        byte[] OriginalBytes = Array.Empty<byte>();
        string[] OriginalLines = Array.Empty<string>();

        public Var(string OriginalFilePath, int OriginalExtension)
        {
            //Based on what extension the original file has, we take the data as a string or as bytes
            if (OriginalExtension == 0)
            {
                OriginalBytes = File.ReadAllBytes(OriginalFilePath);
            }
            else if (OriginalExtension == 1)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                OriginalLines = File.ReadAllLines(OriginalFilePath, Encoding.UTF8);
            }
            //Proceed to check if the file is an actual .VAR, and if not, it throws an exception
            if (Verify(OriginalExtension) == -1)
            {
                throw new Exception("File " + Path.GetFileName(OriginalFilePath) + " is not formatted correctly, make sure that you have a valid file.");
            }
        }

        //Function that return 0 if the corresponding file is an actual .VAR file, if not it returns a -1
        protected int Verify(int Extension)
        {
            switch (Extension)
            {
                //The file is in .var format
                case 0:
                    if (BitConverter.ToInt32(OriginalBytes, (int)OriginalBytes.Length - 5).CompareTo(0x444E455B) == 0
                        && OriginalBytes[OriginalBytes.Length - 1] == 0x5D)
                    {
                        return 0;
                    }
                    else
                    {
                        return -1;
                    }
                //The file is in .txt format
                case 1:
                    {
                        if (OriginalLines[OriginalLines.Length - 1] == "[END]")
                        {
                            return 0;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                default:
                    return -1;
            }
        }

        //VAR files are simply a list of variables. Their structure is as follows: Variable's length (2 bytes) + Variable data (Variable's length bytes).
        //This pattern gets repeated until the penultimate line of the file, where it ends with an [END]
        public void Decompile(string NewFilePath, string NewFileName)
        {
            int CurrentOffset = 0;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            StringBuilder stringbuilder = new StringBuilder();

            while (CurrentOffset < OriginalBytes.Length)
            {
                int VariableLength = BitConverter.ToInt16(OriginalBytes, CurrentOffset);
                CurrentOffset += 2;
                string VariableData = Encoding.GetEncoding("shift-jis").GetString(OriginalBytes, CurrentOffset, VariableLength);
                CurrentOffset += VariableLength;
                stringbuilder.Append(VariableData);
                stringbuilder.Append(Environment.NewLine);
            }

            //Removes the last white line of the StringBuilder
            while (stringbuilder.Length > 0 && char.IsWhiteSpace(stringbuilder[stringbuilder.Length - 1]))
            {
                stringbuilder.Length--;
            }

            File.AppendAllText(Path.Combine(NewFilePath, NewFileName) + ".txt", stringbuilder.ToString());
        }

        //Function that creates a .VAR from the given input file.
        public void Compile(string NewFilePath, string NewFileName)
        {
            int NewFileLength = 0;
            short[] LineLength = new short[OriginalLines.Length];

            //First we count the number of bytes the file has in total
            for (int CurrentLine = 0; CurrentLine < OriginalLines.Length; CurrentLine++)
            {
                LineLength[CurrentLine] = (short)Encoding.GetEncoding("shift-jis").GetByteCount(OriginalLines[CurrentLine]);
                NewFileLength += LineLength[CurrentLine];
            }

            byte[] Data = new byte[NewFileLength + (2 * OriginalLines.Length)];
            int CurrentOffset = 0;

            //Now we go through each line and get their byte count (2 bytes), and then get the actual data in SHIFT-JIS
            for (int CurrentLine = 0; CurrentLine < OriginalLines.Length; CurrentLine++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(LineLength[CurrentLine]), 0, Data, CurrentOffset, 2);
                byte[] CurrentLineBytes = Encoding.GetEncoding("shift-jis").GetBytes(OriginalLines[CurrentLine]);
                Buffer.BlockCopy(CurrentLineBytes, 0, Data, CurrentOffset + 2, LineLength[CurrentLine]);
                CurrentOffset += LineLength[CurrentLine] + 2;
            }

            File.WriteAllBytes(Path.Combine(NewFilePath, NewFileName) + ".var", Data);
        }
    }
}
