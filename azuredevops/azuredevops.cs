using System.Dynamic;

namespace azure
{
    class Builder : IBuilder
    {

        StringWriter writer = new StringWriter();

        public TargetType Target => TargetType.AzureDevOps;

        public IJob job(string JobName, jobFunc func)
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
        List<string> steps = new List<string>();

        public Job(TextWriter tw, string JobName, jobFunc f)
        {
            writer = tw;

            StringWriter localWriter = new StringWriter();
            var ctx = new JobContext(this, localWriter);
            f(ctx);
            tw.WriteLine($"steps:");
            tw.Write(localWriter.ToString());
        }

        public void addStep(string StageName)
        {
            steps.Add(StageName);
        }

        public IJob andThen(jobFunc f)
        {
            var ctx = new JobContext(this, writer);
            f(ctx);
            return this;
        }
    }

    class JobContext : IJobContext
    {
        TextWriter writer;
        Job origin;

        public JobContext(Job origin, TextWriter writer)
        {
            this.writer = writer;
            this.origin = origin;
        }

        public void andThen(anyFunc func)
        {
            throw new NotImplementedException();
        }

        public IJobContext step(string stepName, stepFunc func)
        {
            var ctx = new StepContext(origin, stepName , writer);
            func(ctx);
            return this;
        }
    }

    class StepContext : IStepContext
    {
        TextWriter writer;
        Job origin;

        String StepName;

        public StepContext(Job j, string stepName, TextWriter writer)
        {
            this.origin = j;
            this.writer = writer;
            this.StepName = stepName;
            origin.addStep(StepName);
        }

        public IStepContext andThen(string taskName, taskFunc func)
        {
            return new StepContext(origin, StepName, writer).task(taskName, func);
        }

        public IStepContext task(string taskName, taskFunc func) 
        {
            TaskContext ctx = new TaskContext(this.writer, StepName, taskName);
            func(ctx);
            ctx.finalize();
            return this;
        }
    }

    class TaskContext : ITaskContext
    {
        TextWriter writer;
        List<IArtifact> consumedArtifacts = new List<IArtifact>();
        Artifact? producedArtifact;

        public TaskContext(TextWriter writer, string StepName, string taskName)
        {
            this.writer = writer;
            writer.WriteLine($"- script: |");
           
            Name = $"{taskName}";
        }

        public string Name {get; private set;}

        public void consumeArtifact(IArtifact artifact)
        {
            if(artifact == null)
            {
                throw new Exception("The artifact passed to 'consumeArtifact' must not be null. Ensure, that the artifact is produced in an earlier step.");
            }
            consumedArtifacts.Add(artifact);
        }

        public IArtifact produceArtifact(string name, List<string> filters)
        {
            if(filters.Count == 0)
            {
                throw new Exception("An artifact must at least contain one entry!");
            }

            if(producedArtifact != null)
            {
                throw new Exception("An artifact can only be produced once per step!");
            }

            producedArtifact = new Artifact(this, name, filters);
            return producedArtifact;
        }

        public void sh(string command)
        {
            writer.WriteLine($"    {command}");
        }

    
        /**
            Finalize will write the last lines of
            the step, i.e. not the script!
        */
        internal void finalize()
        { 
            writer.WriteLine($"  displayName: {Name}");
            // if(consumedArtifacts.Any())
            // {
            //     writer.Write("  needs: [");
            //     var numWritten = 0;
            //     foreach (var artifact in consumedArtifacts)
            //     {
            //         writer.Write($"{artifact.producedBy.Name}");
            //         numWritten++;
            //         if (numWritten != consumedArtifacts.Count)
            //         {
            //             writer.Write(",");
            //         }
            //     }
            //     writer.WriteLine("]");
            // }

            // if (producedArtifact != null)
            // {
            //     writer.WriteLine("  artifacts:");
            //     writer.WriteLine("      paths:");
            //     foreach (var path in producedArtifact.Path)
            //     {
            //         writer.WriteLine($"          - {path}");
            //     }
            // }
        }
    }

    class Artifact : IArtifact
    {
        public Artifact(ITaskContext producedBy, string name, List<string> paths)
        {
            Name = name;
            Path = paths;
            this.producedBy = producedBy;
        }

        public string Name {get; private set;}
        public List<string> Path {get; private set;}

        public ITaskContext producedBy{get; private set;}

    }
}
