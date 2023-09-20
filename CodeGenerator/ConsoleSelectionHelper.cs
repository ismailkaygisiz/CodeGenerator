public class ConsoleSelectionHelper
{
    public Option SelectOption(List<Option> options, string message)
    {
        Console.Clear();
        Console.WriteLine(message);
        Console.WriteLine("\nUse ⬆️  and ⬇️  to navigate and press \u001b[32mEnter/Return\u001b[0m to select:");
        (int left, int top) = Console.GetCursorPosition();
        var index = 1;
        var decorator = "✅ \u001B[36m";
        ConsoleKeyInfo key;
        bool isSelected = false;

        while (!isSelected)
        {
            Console.SetCursorPosition(left, top);

            for (int i = 0; i < options.Count; i++)
                Console.WriteLine($"{(index == i + 1 ? decorator : "   ")} {options[i].Name}\u001b[0m");

            key = Console.ReadKey(false);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    index = index == 1 ? options.Count : index - 1;
                    break;

                case ConsoleKey.DownArrow:
                    index = index == options.Count ? 1 : index + 1;
                    break;

                case ConsoleKey.Enter:
                    isSelected = true;
                    break;
            }
        }
        Console.WriteLine($"\n{decorator}You selected Option {options[index - 1].Name}");
        Console.ResetColor();
        options[index - 1].Index = index;
        return options[index - 1];
    }
}
