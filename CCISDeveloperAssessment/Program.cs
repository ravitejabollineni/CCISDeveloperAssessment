using CCISDeveloperAssessment;

// The pound sign in the change breakdown needs a UTF-8 capable console.
try
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
}
catch (IOException)
{
    // Output is redirected or the console does not support the change; carry on regardless.
}

new ConsoleMenu(Console.In, Console.Out).Run();
