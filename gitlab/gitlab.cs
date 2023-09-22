using System.IO;

namespace gitlab 
{
class Builder : IBuilder
{
    
    StringWriter writer = new System.IO.StringWriter();

    public IJob job(string JobName, jobfunc func)
    {
        return new Job(writer, JobName, func);
    }

    public override string ToString()
    {
        return writer.ToString();
    }
    }

class Job : IJob
{
    TextWriter writer;
    List<String> stageList = new List<string>();

    public Job(TextWriter tw, string JobName, jobfunc f)
    { 
        writer = tw;
        
        StringWriter localWriter = new StringWriter();
        var ctx = new Context(this, localWriter);
        f(ctx);
        tw.WriteLine($"stages:");
        foreach(var s in stageList)
        {
            tw.WriteLine($" -{s}");
        }

        tw.Write(localWriter.ToString());
    }

    public void addStage(string StageName)
    {
        stageList.Add(StageName);
    }

    public IJob andThen(jobfunc f)
    {
        var ctx = new Context(this, writer);
        f(ctx);
        return this;
    }
}

    class Context : IContext
    {
        TextWriter writer;
        Job origin; 

        public Context(Job j, TextWriter writer)
        {
            this.origin = j;
            this.writer = writer;
        }

        public IContext andThen(anyFunc func)
        {
            func();
            return this;
        }

        public IContext stage(string stageName, stagefunc func)
        {
            origin.addStage(stageName);
            Stage s = new Stage(this.writer, stageName);
            func(s);
            return this;
        }
    }

    class Stage : IStageContext
    {
        TextWriter writer;

        public Stage(TextWriter writer, string StageName)
        {
            this.writer = writer;
            writer.WriteLine($"{StageName}-Job:");
            writer.WriteLine($"  stage:{StageName}");
            writer.WriteLine($"  script:");
        }

        public void sh(string command)
        {
            writer.WriteLine($"     - {command}");
        }
    }
}
