namespace gitlab
{
    class Builder : IBuilder
    {

        StringWriter writer = new StringWriter();

        public TargetType Target => TargetType.Gitlab;

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
            tw.WriteLine($"stages:");
            foreach (var s in steps)
            {
                tw.WriteLine($" - {s}");
            }

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

        public IJobContext step(string stepName, stageFunc func)
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

        public IStepContext andThen(anyFunc func)
        {
            func();
            return this;
        }

        public void task(string taskName, taskFunc func) 
        {
            TaskContext ctx = new TaskContext(this.writer, StepName, taskName);
            func(ctx);
        }
    }

    class TaskContext : ITaskContext
    {
        TextWriter writer;

        public TaskContext(TextWriter writer, string StepName, string taskName)
        {
            this.writer = writer;
            writer.WriteLine($"{taskName}-Job:");
            writer.WriteLine($"  stage: {StepName}");
            writer.WriteLine($"  script:");
        }

        public void sh(string command)
        {
            writer.WriteLine($"     - {command}");
        }
    }
}
