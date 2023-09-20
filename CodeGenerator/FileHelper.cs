using System.Text;

namespace CodeGenerator
{
    public class FileHelper
    {
        public static string ReadFile(string path) => File.ReadAllText(path, Encoding.UTF8);

        public static void CreateAndWriteFile(string path, string content) => File.WriteAllText(path, content);
    }
}
