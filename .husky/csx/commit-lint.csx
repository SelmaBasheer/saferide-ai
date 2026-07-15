using System.Text.RegularExpressions;
private var pattern = @"^(feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\([a-z0-9-]+\))?(!)?: .{1,80}";
private var msg = File.ReadAllLines(Args[0])[0];
if (Regex.IsMatch(msg, pattern)) return 0;
Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine("Invalid commit message. Expected: type(scope): subject");
return 1;