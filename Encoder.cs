using System.Text;

namespace Huffman
{
    using Constants;
    using HuffmanTree;
    using Extensions;
    class Encoder
    {
        /// <summary>
        ///     Encrypts file using Huffman encoding. 
        ///     Creates new file with .huff suffix.
        /// </summary>
        /// <param name="file"> Path to file to encrypt. </param>
        public void EncryptFile(string file)
        {
            string outputFileName = file + ".huff";
            BinaryReader reader;
            BinaryWriter writer;
            try
            {
                reader = new BinaryReader(File.Open(file, FileMode.Open));
                writer = new BinaryWriter(File.Open(outputFileName, FileMode.Create));
            }
            catch (Exception)
            {
                Console.WriteLine("File Error");
                return;
            }
            Node? root = Node.BuildHuffmanTreeFromArray(FindSymbolsFrequencies(reader));
            writeHeader(writer);
            if (root != null)
            {
                writeEncryptedTree(writer, root);
                long treeDescriptionTerminal = 0; // 64-bit 0
                writer.Write(BitConverter.GetBytes(treeDescriptionTerminal));
                string[] paths = Node.MapPathsToLeaves(root);
                writeEncryptedData(writer, reader, paths);
            }
            writer.Close();
            reader.Close();
        }

        private long[] FindSymbolsFrequencies(BinaryReader reader)
        {
            long[] frequencies = new long[Constants.AlphabetSize];
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            byte[] characters;
            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                characters = reader.ReadBytes(Constants.ChunkSize);
                foreach (var character in characters)
                    frequencies[character]++;
            }
            return frequencies;
        }

        private void writeHeader(BinaryWriter writer)
        {
            byte[] header = { 0x7B, 0x68, 0x75, 0x7C, 0x6D, 0x7D, 0x66, 0x66 };
            writer.Write(header);
        }

        private void writeEncryptedTree(BinaryWriter writer, Node root)
        {
            if (root is LeafNode)
            {
                writer.Write(root.GetEncryptedValue());
                return;
            }
            else if (root is InnerNode)
            {
                writer.Write(root.GetEncryptedValue());
                writeEncryptedTree(writer, ((InnerNode)root).Left!);
                writeEncryptedTree(writer, ((InnerNode)root).Right!);
            }
        }

        private void writeEncryptedData(BinaryWriter writer, BinaryReader reader, string[] paths)
        {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            byte[] symbols;
            string remainingBits = "";
            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                StringBuilder bitSequence = new StringBuilder();
                bitSequence.Append(remainingBits);
                symbols = reader.ReadBytes(Constants.ChunkSize);
                foreach (var symbol in symbols)
                {
                    bitSequence.Append(paths[symbol]);
                }
                byte[] bytes = new byte[bitSequence.Length / 8];

                for (int i = 0; i <= (bitSequence.Length - 8); i += 8)
                {
                    string byteString = bitSequence.ToString(i, 8).Reverse();
                    bytes[i / 8] = Convert.ToByte(byteString, 2);
                }
                writer.Write(bytes);
                remainingBits = bitSequence.ToString(bitSequence.Length - (bitSequence.Length % 8), (bitSequence.Length % 8));
            }
            if (remainingBits.Length > 0)
            {
                remainingBits = remainingBits.Reverse();
                byte lastByte = Convert.ToByte(remainingBits.PadLeft(8, '0'), 2);
                writer.Write(lastByte);
            }
        }
    }
}
