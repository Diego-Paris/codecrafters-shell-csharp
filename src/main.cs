class Program
{
    static void Main()
    {
        while (true)
        {
            Console.Write("$ ");
            var input = Console.ReadLine();

            Console.WriteLine($"{input}: command not found");
        }
    }
}
