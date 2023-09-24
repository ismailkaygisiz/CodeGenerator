using CodeGenerator;
using System.Text;

Console.Clear();
Console.OutputEncoding = Encoding.UTF8;
Console.CursorVisible = false;
Console.WriteLine("Welcome to the \u001b[31mCode Generator\u001b[0m");
Console.ResetColor();


Generator generator = new Generator();
ConsoleSelectionHelper selectionHelper = new ConsoleSelectionHelper();

string projectName = "";
var solution = generator.DetectFiles("", ".sln").FirstOrDefault()?.Split("\\");
if (solution != null)
{
    var file = solution[solution.Length - 1];
    Console.Write($"I found a solution file do you want to use that name : {file.Split(".")[0]}\n\u001b[32m[Y/n]\u001b[0m >> ");
    var opt = Console.ReadLine();
    projectName = file.Split(".")[0];
    if (opt.ToLower() == "n")
        projectName = Console.ReadLine();
}

if (string.IsNullOrEmpty(projectName))
{
    Console.Write("Please input project name >> ");
    projectName = Console.ReadLine();
    if (string.IsNullOrEmpty(projectName))
        throw new Exception("Project name cannot be empty");
}

var directory = "Domain\\Entities";
var objects = generator.DetectObjects(directory, ".cs");
List<Option> options = new List<Option>();
foreach (var obj in objects)
    options.Add(new Option() { Name = obj.ClassName });

List<Option> idOptions = new List<Option>
{
    new Option() { Name = "int" },
    new Option() { Name = "long" },
    new Option() { Name = "string" },
    new Option() { Name = "Guid" },
    new Option() { Name = "object" }
};

List<Option> typeOptions = new List<Option>
{
    new Option() { Name = "Repository" },
    new Option() { Name = "Service" },
    new Option() { Name = "Feature" },
    new Option() { Name = "RepositoryWithService" },
    new Option() { Name = "RepositoryWithFeature" },
    new Option() { Name = "All" }
};

var id = selectionHelper.SelectOption(idOptions, "Please select your type of id >> ");
var option = selectionHelper.SelectOption(options, "Please select your entity >> ");
var typeOption = selectionHelper.SelectOption(typeOptions, "Please select your option >> ");

Console.WriteLine($"Your Options\n>> {projectName}\n>> {id.Name}\n>> {option.Name}\n>> {typeOption.Name}\n");
generator.Generate(option.Name, typeOption.Name, projectName, id.Name, objects);
Console.ResetColor();
