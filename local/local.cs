using System.Diagnostics;
using System.Dynamic;

namespace local
{

    class Builder : IBuilder
    {
        static int activeTasks = 0;
        static AutoResetEvent tasksDone = new AutoResetEvent(false);

        public static  void AddActiveTask()
        {
            Interlocked.Increment(ref activeTasks);
        }

        public static void RemoveActiveTask()
        {
            Interlocked.Decrement(ref activeTasks);
            if(activeTasks == 0)
            {
                Builder.tasksDone.Set();
            }
        }


        StringWriter writer = new StringWriter();

        public TargetType Target => TargetType.Local;

        public IJob job(string JobName, jobFunc func)
        {
            return new Job(writer, JobName, func);
        }

        public override string ToString()
        {
            tasksDone.WaitOne();
            return "Local Execution finished";
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

        AutoResetEvent evt = new AutoResetEvent(false);

        public StepContext(Job j, string stepName, TextWriter writer)
        {
            this.origin = j;
            this.writer = writer;
            this.StepName = stepName;
            origin.addStep(StepName);            
        }

        ~StepContext()
        {
            evt.Set();
        }

        void runTask(string taskName, taskFunc func)        
        {
            Builder.AddActiveTask();
            evt.Reset();
            
            ThreadPool.QueueUserWorkItem( state => {
                
                try
                {
                    TaskContext ctx = new TaskContext(this.writer, StepName, taskName);
                    func(ctx);
                    ctx.finalize();
                    evt.Set();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                Builder.RemoveActiveTask();
            });
        }

        public IStepContext andThen(string taskName, taskFunc func)
        {
            try
            {   
                // this is a hack to avoid early termination.
                // we indicate to the builder, that there mighte still be
                // tasks generated...
                Builder.AddActiveTask();
                evt.WaitOne();
                return new StepContext(origin, StepName, writer).task(taskName, func);
            } 
            finally
            {
                Builder.RemoveActiveTask();
            }
            
        }

        public IStepContext task(string taskName, taskFunc func) 
        {
            runTask(taskName, func);
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
            writer.WriteLine($"{taskName}:");
            writer.WriteLine($"  stage: {StepName}");
            writer.WriteLine($"  script:");
            Name = $"{taskName}";
        }

        public string Name {get; private set;}

        public void consumeArtifact(IArtifact artifact)
        {
            if(artifact == null)
            {
                throw new Exception($"The artifact passed to 'consumeArtifact' in task '{Name}' must not be null. Ensure, that the artifact is produced in an earlier step.");
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
            writer.WriteLine($"     - {command}");
            // Create a new process
            Process process = new Process();

            // Set the command to execute
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/C {command}";
             // Redirect the output to the console
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;

            // Start the process
            process.Start();

            // Read the output
            string output = process.StandardOutput.ReadToEnd();

            // Wait for the process to exit
            process.WaitForExit();

            // Print the output
            Console.WriteLine(output);
        }

    
        /**
            Finalize will write the last lines of
            the step, i.e. not the script!
        */
        internal void finalize()
        {
            if(consumedArtifacts.Any())
            {
                writer.Write("  needs: [");
                var numWritten = 0;
                foreach (var artifact in consumedArtifacts)
                {
                    writer.Write($"{artifact.producedBy.Name}");
                    numWritten++;
                    if (numWritten != consumedArtifacts.Count)
                    {
                        writer.Write(",");
                    }
                }
                writer.WriteLine("]");
            }

            if (producedArtifact != null)
            {
                writer.WriteLine("  artifacts:");
                writer.WriteLine("      paths:");
                foreach (var path in producedArtifact.Path)
                {
                    writer.WriteLine($"          - {path}");
                }
            }
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
