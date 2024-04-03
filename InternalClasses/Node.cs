using System.Text;


namespace HuffmanTree
{
    using Constants;
    using Extensions;
    public abstract class Node
    {
        public long Weight { get; set; }
        public abstract byte[] GetEncryptedValue();
        public override string ToString() => Weight.ToString();
        public static string[] MapPathsToLeaves(Node root)
        {
            void Dfs(Node currentNode, string path, string[] foundPaths)
            // inner recursive function to traverse the tree and find paths to leaves
            {
                if (currentNode is LeafNode)
                {
                    foundPaths[((LeafNode)currentNode).Symbol] = path;
                    return;
                }
                else if (currentNode is InnerNode)
                {
                    Dfs(((InnerNode)currentNode).Left!, path + "0", foundPaths);
                    Dfs(((InnerNode)currentNode).Right!, path + "1", foundPaths);
                }
            }

            string[] paths = new string[Constants.AlphabetSize];
            Dfs(root, "", paths);
            return paths;
        }
        public static Node? BuildHuffmanTreeFromArray(long[] frequencies)
        {
            List<Node> nodes = new List<Node>();
            for (int i = 0; i < Constants.AlphabetSize; i++)
            {
                if (frequencies[i] > 0)
                {
                    nodes.Add(new LeafNode()
                    {
                        Symbol = i,
                        Weight = frequencies[i]
                    });
                }
            }
            if (nodes.Count == 0)
                return null;

            int id = 0;
            while (nodes.Count > 1)
            {
                nodes.Sort(new NodePriorityComparer());
                var left = nodes[0];
                var right = nodes[1];
                InnerNode innerNode = new InnerNode { Id = id, Left = left, Right = right, Weight = left.Weight + right.Weight };
                nodes.RemoveAt(0);
                nodes.RemoveAt(0);
                nodes.Add(innerNode);
                ++id;
            }
            return nodes[0];
        }
    }

    class LeafNode : Node
    {
        public int Symbol { get; set; }
        public override string ToString() => $"*{Symbol}:{Weight}";
        public override byte[] GetEncryptedValue()
        {
            string binaryWeight = Convert.ToString(Weight, 2);
            string binarySymbol = Convert.ToString(Symbol, 2);
            StringBuilder sb = new StringBuilder();
            sb.Append('1');
            sb.Append(binaryWeight.Reverse().PadRight(55, '0'));
            sb.Append(binarySymbol.Reverse().PadRight(8, '0'));
            byte[] bytes = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                bytes[i] = Convert.ToByte(sb.ToString(i * 8, 8).Reverse(), 2);
            }
            return bytes;
        }
    }

    class InnerNode : Node
    {
        public int Id { get; set; }
        public Node? Left { get; set; }
        public Node? Right { get; set; }
        public override string ToString() => $"{Weight} {Left} {Right}";        
        public override byte[] GetEncryptedValue()
        {
            string binaryWeight = Convert.ToString(Weight, 2);
            StringBuilder sb = new StringBuilder();
            sb.Append('0');
            sb.Append(binaryWeight.Reverse().PadRight(63, '0'));
            byte[] bytes = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                bytes[i] = Convert.ToByte(sb.ToString(i * 8, 8).Reverse(), 2);
            }
            return bytes;
        }
    }

    public class NodePriorityComparer : IComparer<Node>
    {
        public int Compare(Node? x, Node? y)
        {
            if (x == null || y == null)
                throw new ArgumentNullException("Trying to compare null");

            int weightComparison;
            if ((weightComparison = x.Weight.CompareTo(y.Weight)) != 0)
                return weightComparison;

            // prioritize LeafNode over InnerNode
            if (x is LeafNode && y is InnerNode)
                return -1;
            else if (x is InnerNode && y is LeafNode)
                return 1;

            // LeafNodes: prioritize lower character value
            if (x is LeafNode leafX && y is LeafNode leafY)
                return leafX.Symbol.CompareTo(leafY.Symbol);

            // InnerNodes: prioritize based on creation order
            if (x is InnerNode innerX && y is InnerNode innerY)
                return innerX.Id.CompareTo(innerY.Id);

            return 0;
        }
    }
}
