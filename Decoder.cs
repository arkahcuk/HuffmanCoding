using System.Text;

namespace Huffman
{
    using Constants;
    using HuffmanTree;
    using Extensions;
    class Decoder
    {
        /// <summary>
        ///     Decrypts file encrypted by Huffman encoder into new file.
        /// </summary>
        /// <param name="file"> Path to encrypted file, must have .huff suffix. </param>
        public void DecryptFile(string file)
        {
            BinaryReader? reader = null;
            BinaryWriter? writer = null;
            try
            {
                reader = new BinaryReader(File.Open(file, FileMode.Open));
                writer = new BinaryWriter(File.Open(file.Substring(0, file.Length - 5), FileMode.Create));

                if (!readHeader(reader))
                    throw new UnsupportedFileFormatException("Unrecognized file header");

                Node? root = decryptTree(reader);

                if (!readTreeDescriptionTerminal(reader))
                    throw new UnsupportedFileFormatException("No termination after tree data");

                if (root != null)
                    decryptData(reader, writer, root);
            }
            catch (Exception)
            {
                Console.WriteLine("File Error");
            }
            finally
            {
                if (writer != null)
                    writer.Close();
                if (reader != null)
                    reader.Close();
            }
        }
        private bool readHeader(BinaryReader reader)
        {
            byte[] header = reader.ReadBytes(8);
            byte[] expectedHeader = { 0x7B, 0x68, 0x75, 0x7C, 0x6D, 0x7D, 0x66, 0x66 };
            for (int i = 0; i < 8; ++i)
            {
                if (header[i] != expectedHeader[i])
                    return false;
            }
            return true;
        }

        private Node? decryptTree(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(8); // one node
            StringBuilder binary = new StringBuilder();
            foreach (var b in bytes)
            {
                binary.Append(Convert.ToString(b, 2).PadLeft(8, '0').Reverse());
            }
            if (binary[0] == '1')
            {
                long weight = Convert.ToInt64(binary.ToString().Substring(1, 55).Reverse(), 2);
                int symbol = Convert.ToInt32(binary.ToString().Substring(56, 8).Reverse(), 2);
                return new LeafNode { Weight = weight, Symbol = symbol };
            }
            else if (binary[0] == '0')
            {
                long weight = Convert.ToInt64(binary.ToString().Substring(1, 63).Reverse(), 2);
                InnerNode innerNode = new InnerNode { Weight = weight };
                innerNode.Left = decryptTree(reader);
                innerNode.Right = decryptTree(reader);
                return innerNode;
            }
            return null;
        }

        private bool readTreeDescriptionTerminal(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(8);
            for (int i = 0; i < 8; ++i)
            {
                if (bytes[i] != 0)
                    return false;
            }
            return true;
        }

        private void decryptData(BinaryReader reader, BinaryWriter writer, Node root)
        {
            long numberOfSymbols = root.Weight;
            List<byte> symbolsToWrite = new List<byte>();
            StringBuilder bitSequence = new StringBuilder();
            Node currentNode = root;
            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                byte[] bytes = reader.ReadBytes(Constants.ChunkSize);
                foreach (var b in bytes)
                {
                    bitSequence.Append(Convert.ToString(b, 2).PadLeft(8, '0').Reverse());
                    while (bitSequence.Length > 0)
                    {
                        if (currentNode is InnerNode)
                        {
                            if (bitSequence[0] == '0')
                                currentNode = ((InnerNode)currentNode).Left!;
                            else if (bitSequence[0] == '1')
                                currentNode = ((InnerNode)currentNode).Right!;
                            bitSequence.Remove(0, 1);
                        }
                        if (currentNode is LeafNode)
                        {
                            symbolsToWrite.Add(Convert.ToByte(((LeafNode)currentNode).Symbol));
                            if (symbolsToWrite.Count == Constants.ChunkSize)
                            {
                                writer.Write(symbolsToWrite.ToArray());
                                symbolsToWrite.Clear();
                            }
                            if (--numberOfSymbols == 0)
                            {
                                writer.Write(symbolsToWrite.ToArray());
                                return;
                            }
                            currentNode = root;
                        }
                    }
                }
            }
        }
    }    
    public class UnsupportedFileFormatException : Exception 
    {
        public UnsupportedFileFormatException(string message) : base(message) { }
    }
}
