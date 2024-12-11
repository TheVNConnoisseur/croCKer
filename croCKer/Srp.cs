using Microsoft.VisualBasic;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Shapes;

namespace croCKer
{
    internal class Srp
    {
        byte[] OriginalBytes = Array.Empty<byte>();
        string[] OriginalLines = Array.Empty<string>();
        public Srp(string OriginalFilePath, int OriginalExtension)
        {
            //Based on what extension the original file has, we take the data as a string or as bytes
            if (OriginalExtension == 0)
            {
                OriginalBytes = File.ReadAllBytes(OriginalFilePath);
            }
            else if (OriginalExtension == 1)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                OriginalLines = File.ReadAllLines(OriginalFilePath, Encoding.GetEncoding("shift-jis"));
            }
            //Proceed to check if the file is an actual .SRP, and if not, it throws an exception
            if (Verify(OriginalExtension) == -1)
            {
                throw new Exception("File " + System.IO.Path.GetFileName(OriginalFilePath) + " is not formatted correctly, make sure that you have a valid file.");
            }
        }

        //Function that return 0 if the corresponding file is an actual .SRP file, if not it returns a -1
        public int Verify(int Extension)
        {
            switch (Extension)
            {
                //The file is in .srp format
                case 0:
                    if (BitConverter.ToInt16(OriginalBytes, OriginalBytes.Length - 2).CompareTo(0x03) == 0
                        && BitConverter.ToInt16(OriginalBytes, OriginalBytes.Length - 4).CompareTo(0x10) == 0)
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
                        if ((OriginalLines.Length + 1) % 4 == 0)
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

        //While decompiling the SRP file, we have to follow this structure. First we gotta obtain the number of
        //instructions the file has (4 bytes). Then, we gotta repeat the following structure the number of times obtained
        //with the result we have obtained before: Instruction length (2 bytes) + Instruction type (2 bytes) + Instruction
        //flags (2 bytes) + Instruction data (Instruction length - 2 - 2 bytes).
        //Instruction length: the length of a the instruction in bytes (the result only tells the amount of bytes the
        //type, flags and data fields have).
        //Instruction type: the type of the command. While we can't be 100% sure for all the games, these are the most important ones
        //that we can know based on their context: 0 is for text inside in-game, 1 is for when the player has to choose an option in-game,
        //2 is for loading images (character sprites of backgrounds) and 3 is for sounds (BGM or SE).
        //Instruction flags: these flags apparently are for adjusting some choices when loading an instruction of a specific type.
        //Instruction data: the bulk of the instruction. If it is text related, it will include the dialog text. Or if it has to load a
        //specific image, here it is where it will make the call. Also something very important, only this section of the file has its
        //nibbles swapped.
        public void Decompile(string NewFilePath, string NewFileName)
        {
            int CurrentOffset = 0;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            StringBuilder stringbuilder = new StringBuilder();

            //We obtain the number of instructions present on the file (4 bytes)
            int NumInstructions = BitConverter.ToInt32(OriginalBytes, 0);
            CurrentOffset += 4;

            //The data portion of the .srp files needs to have swapped its nibles
            byte[] SwappedBytes = SwapNibblesInByteArray(OriginalBytes);

            //We iterate through the entire file until we parse the last instruction
            for (int CurrentInstruction = 0; CurrentInstruction < NumInstructions; CurrentInstruction++)
            {
                short InstructionLength = BitConverter.ToInt16(OriginalBytes, CurrentOffset);
                CurrentOffset += 2;
                short InstructionType = BitConverter.ToInt16(OriginalBytes, CurrentOffset);
                CurrentOffset += 2;
                stringbuilder.AppendLine("Type: " + InstructionType);
                short InstructionFlags = BitConverter.ToInt16(OriginalBytes, CurrentOffset);
                CurrentOffset += 2;
                stringbuilder.AppendLine("Flags: " + InstructionFlags);

                //Not all instructions contain any kind of data, so first we need to ensure that
                //the current instruction has any of it
                if (InstructionLength > 4)
                {
                    //Some instructions start with \t, \r\n or even \n, that must be removed in
                    //order to keep the file structure intact, and those can be safely trimmed out
                    string InstructionData = Encoding.GetEncoding("shift-jis").
                        GetString(SwappedBytes, CurrentOffset, InstructionLength - 2 - 2).
                        TrimStart('\t', '\r', '\n');
                    CurrentOffset += InstructionLength - 2 - 2;
                    stringbuilder.AppendLine("Data: " + InstructionData);
                }
                else
                {
                    stringbuilder.AppendLine("Data: ");
                }

                //Put an empty line for having a more readable output file
                stringbuilder.Append(Environment.NewLine);

                //Apparently the header, even though it indicates the number of instructions,
                //the game engine does not even follow said value, it just checks for when the footer
                //instruction comes up.
                //In some edge cases, this value is set wrong by the developers,
                //causing an EndofStreamException. Instead of detecting said exception, to avoid issues
                //with incorrectly formatted files modified by any user previously, it will be checked
                //if the current offset coincides with the length of the file, because it should mean
                //that the file has been read correctly and that there are no more instructions to read.
                if (CurrentOffset == OriginalBytes.Length)
                {
                    break;
                }
            }

            //Remove the last /r/n characters from the string in order to take out the last empty line
            stringbuilder.Remove(stringbuilder.Length - 4, 4);
            
            File.WriteAllText(System.IO.Path.Combine(NewFilePath, NewFileName) + ".txt", stringbuilder.ToString(), Encoding.GetEncoding("shift-jis"));
        }

        //Funcion that generates the compiled .SRP file. The process of creating one is done by following the same pattern as
        //for decompiling a .SRP file.
        public void Compile(string NewFilePath, string NewFileName)
        {
            //We start with 4 since it has a header that includes the amount of number of instructions
            int NewFileLength = 4;

            //Four lines per instruction, and we have to take into consideration that the last instruction has its
            //empty line removed
            int NumofInstructions = (OriginalLines.Length + 1) / 4;
            short[] InstructionLength = new short[NumofInstructions];
            short[] InstructionType = new short[NumofInstructions];
            short[] InstructionFlags = new short[NumofInstructions];
            byte[][] InstructionData = new byte[NumofInstructions][];

            //First we get the length of each instruction, while at the same time the total length of the file
            for (int CurrentInstruction = 0; CurrentInstruction < NumofInstructions; CurrentInstruction++)
            {
                //We take out the strings generated when exporting the original .srp file
                OriginalLines[CurrentInstruction * 4] = OriginalLines[CurrentInstruction * 4].Replace("Type: ", "");
                OriginalLines[(CurrentInstruction * 4) + 1] = OriginalLines[(CurrentInstruction * 4) + 1].Replace("Flags: ", "");
                OriginalLines[(CurrentInstruction * 4) + 2] = OriginalLines[(CurrentInstruction * 4) + 2].Replace("Data: ", "");
                InstructionLength[CurrentInstruction] = Convert.ToInt16(Encoding.GetEncoding("shift-jis").
                    GetByteCount(OriginalLines[(CurrentInstruction * 4) + 2]) + 2 + 2);
                InstructionType[CurrentInstruction] = Convert.ToInt16(OriginalLines[CurrentInstruction * 4]);
                InstructionFlags[CurrentInstruction] = Convert.ToInt16(OriginalLines[(CurrentInstruction * 4) + 1]);
                if (InstructionLength[CurrentInstruction] - 2 - 2 != 0)
                {
                    InstructionData[CurrentInstruction] = SwapNibblesInByteArray(Encoding.GetEncoding("shift-jis").
                        GetBytes(OriginalLines[(CurrentInstruction * 4) + 2]));
                }

                //We also have to take into consideration the 2 bytes that are reserved for the instruction's length, since those
                //don't count towards the instruction's length, but they do towards the file's total length
                NewFileLength += InstructionLength[CurrentInstruction] + 2;
            }

            byte[] Data = new byte[NewFileLength];
            int CurrentOffset = 0;

            //Now we go through each line again, but this time we take out their information and fill in the array of bytes
            //that will conform the final file

            //First we get the total number of instructions
            Buffer.BlockCopy(BitConverter.GetBytes(NumofInstructions), 0, Data, 0, 4);
            CurrentOffset += 4;

            for (int CurrentInstruction = 0; CurrentInstruction < NumofInstructions; CurrentInstruction++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(InstructionLength[CurrentInstruction]), 0, Data, CurrentOffset, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(InstructionType[CurrentInstruction]), 0, Data, CurrentOffset + 2, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(InstructionFlags[CurrentInstruction]), 0, Data, CurrentOffset + 2 + 2, 2);
                if (InstructionLength[CurrentInstruction] - 2 - 2 != 0)
                {
                    Buffer.BlockCopy(InstructionData[CurrentInstruction], 0, Data, CurrentOffset + 2 + 2 + 2, InstructionLength[CurrentInstruction] - 2 - 2);
                }
                CurrentOffset += InstructionLength[CurrentInstruction] + 2;
            }

            File.WriteAllBytes(System.IO.Path.Combine(NewFilePath, NewFileName) + ".srp", Data);
        }

        //For those on the unknown, swapping nibbles in a byte is done by left shifting the original byte by 4 bits to the
        //left (lower nibble to the high position), and then the same but to the right (higher nibble to the low position).
        //Once that's done, we obtain the desired byte with an OR (|) operation.
        //Example with 0xAB (10101011):
        //10110000 (to the left 4 bits)
        //00001010 (to the right 4 bits)
        //10111010 (0xBA)
        public static byte SwapNibbles(byte OriginalByte)
        {
            return (byte)((OriginalByte << 4) | (OriginalByte >> 4));
        }

        //Swapping nibbles in the entire data array might be ineffective, but considering the expected size of .srp files is
        //pretty small, it will be kept this way for readability's sake
        public static byte[] SwapNibblesInByteArray(byte[] OriginalData)
        {
            return OriginalData.Select(SwapNibbles).ToArray();
        }
    }
}
