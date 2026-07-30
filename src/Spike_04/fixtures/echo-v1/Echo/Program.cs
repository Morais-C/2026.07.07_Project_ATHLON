// Spike_04 fixture baseline: read a line, echo it back (read → process → print).
Console.Write("Enter text: ");
var input = Console.ReadLine() ?? string.Empty;
Console.WriteLine(input);
