using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Security.Policy;
using System.Text;
using Figgle;
using System.Xml.Linq;

Console.WriteLine(
    FiggleFonts.Standard.Render("SQL Linter")
);

Console.WriteLine();

if (args.Count() == 0)
    Console.WriteLine("+ Falta al menos un nombre de archivo a analizar o una carpeta");

var results = new List<LinterResult>();
var files = new List<string>();

foreach (var arg in args)
{
    FileAttributes attr = File.GetAttributes(arg);
    if (attr == FileAttributes.Directory)
    {
        files.AddRange(Directory.GetFiles(arg, "*.sql"));
    } 
    else
    {
        files.Add(arg);
    }
}

foreach (var file in files)
{
    Console.Write($"\n+ Analizando: {file}. ");

    var result = linter(file);

    results.Add(result);

    if (result.ErrorCount == 0)
    {
        Console.WriteLine("OK");
    }
    else
    {
        if (result.ErrorCount == 1)
            Console.WriteLine($"{result.ErrorCount} error detectado");
        else
            Console.WriteLine($"{result.ErrorCount} errores detectados");

        foreach (var error in result.Errors)
        {
            Console.WriteLine($"    {error.Clean()}");
        }
    }
}


Console.WriteLine("\n√ Analisis finalizado");

LinterResult linter(string path)
{
    TextReader? reader = null;
    try
    {
        reader = new StreamReader(path);
        var result = linterFromReader(reader);
        result.FileName = path;
        return result;
    }
    catch (FileNotFoundException ex)
    {
        return LinterResult.Error(path, ex.Message);
    }
    catch (Exception ex)
    {
        return LinterResult.Error(path, ex.Message);
    }    
}

LinterResult linterFromReader(TextReader reader) {

    TSql110Parser parser = new TSql110Parser(true);
    TSqlFragment sqlFragment = parser.Parse(reader, out IList<ParseError> errors);

    LinterResult result;

    if (errors.Count > 0)
    {
        result = LinterResult.Error("");
        foreach (var error in errors)
        {
            result.Errors.Add($"{error.Message} en linea {error.Line}");
        }
        return result;
    }

    SQLVisitor visitor = new SQLVisitor();
    sqlFragment.Accept(visitor);

    return visitor.GetResult();
}

class LinterResult
{
    public string? FileName { get; set; }
    public int ErrorCount { get => Errors.Count(); }
    public List<string> Errors { get; set; } = new List<string>();

    public static LinterResult Ok(string fileName)
    {
        return new LinterResult()
        {
            FileName = fileName,
        };
    }
    public static LinterResult Error(string fileName, string error)
    {
        var result = new LinterResult()
        {
            FileName = fileName,
        };
        result.Errors.Add(error);
        return result;
    }
    public static LinterResult Error(string fileName)
    {
        var result = new LinterResult()
        {
            FileName = fileName,
        };
        return result;
    }
    public LinterResult AddError(string error)
    {
        Errors.Add(error);
        return this;
    }
}

class SQLVisitor : TSqlFragmentVisitor
{
    LinterResult result = LinterResult.Ok("");

    bool beginTrans = false;
    int? beginTransLine = null;

    private string GetNodeTokenText(TSqlFragment fragment)
    {
        StringBuilder tokenText = new StringBuilder();
        for (int counter = fragment.FirstTokenIndex; counter <= fragment.LastTokenIndex; counter++)
        {
            tokenText.Append(fragment.ScriptTokenStream[counter].Text);
        }
        return tokenText.ToString();
    }
    public override void ExplicitVisit(UpdateStatement node)
    {
        var stm = GetNodeTokenText(node);
        if (!stm.Contains("where", StringComparison.InvariantCultureIgnoreCase))
        {
            result.AddError($"Linea: {node.StartLine}: {stm.ToUpper()}\n");
        }

    }
    public override void ExplicitVisit(DeleteStatement node)
    {
        var stm = GetNodeTokenText(node);
        if (!stm.Contains("where", StringComparison.InvariantCultureIgnoreCase))
        {
            result.AddError($"Linea: {node.StartLine}: {stm.ToUpper()}\n");
        }
    }
    public override void ExplicitVisit(DropTableStatement node)
    {
        var stm = GetNodeTokenText(node);
        result.AddError($"Linea: {node.StartLine}: {stm.ToUpper()}\n");
    }
    public override void ExplicitVisit(TruncateTableStatement node)
    {
        var stm = GetNodeTokenText(node);
        result.AddError($"Linea: {node.StartLine}: {stm.ToUpper()}\n");
    }

    public override void ExplicitVisit(BeginTransactionStatement node)
    {
        var stm = GetNodeTokenText(node);
        beginTransLine = node.StartLine;
        beginTrans = true;
    }
    public override void ExplicitVisit(CommitTransactionStatement node)
    {
        if (!beginTrans)
            result.AddError($"Linea: {node.StartLine}: COMMIT sin BEGIN TRAN \n");

        beginTrans = false;
        beginTransLine = null;
    }
    public override void ExplicitVisit(RollbackTransactionStatement node)
    {
        if (!beginTrans)
            result.AddError($"Linea: {node.StartLine}: ROLLBACK sin BEGIN TRAN \n");

        beginTrans = false;
        beginTransLine = null;
    }
    public LinterResult GetResult() 
    {
        if (beginTrans)
            result.AddError($"Linea: {beginTransLine}: BEGIN TRAN sin COMMIT ni ROLLBACK \n");
        return result;
    }

}

static class StringExtensions
{
    public static string Clean(this string value)
    {
        return value.Replace("\n", "").Replace("\r", "").Replace("\t", " ").Replace("  ", "");
    }
}