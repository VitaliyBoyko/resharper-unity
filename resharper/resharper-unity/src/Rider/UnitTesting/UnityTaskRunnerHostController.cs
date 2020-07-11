using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Application.Components;
using JetBrains.ReSharper.Host.Features.Unity;
using JetBrains.ReSharper.TaskRunnerFramework;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Launch;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    public class UnityTaskRunnerHostController : ITaskRunnerHostController
    {
        private const string NotAvailableUnityEditorMessage = "Unity Editor is not available";

        private readonly IUnityController myUnityController;
        private readonly ITaskRunnerHostController myInnerHostController;

        public UnityTaskRunnerHostController(ITaskRunnerHostController innerHostController,
            IUnityController unityController)
        {
            myInnerHostController = innerHostController;
            myUnityController = unityController;
        }

        public void Dispose() => myInnerHostController.Dispose();

        public string HostId => myInnerHostController.HostId;

        public void SupplementContainer(ComponentContainer container) 
            => myInnerHostController.SupplementContainer(container);

        public Task AfterLaunchStarted() => myInnerHostController.AfterLaunchStarted();

        public Task BeforeLaunchFinished() => myInnerHostController.BeforeLaunchFinished();

        public ClientControllerInfo GetClientControllerInfo(IUnitTestRun run) 
            => myInnerHostController.GetClientControllerInfo(run);
        
        public Task CleanupAfterRun(IUnitTestRun run) => myInnerHostController.CleanupAfterRun(run);

        public void Cancel(IUnitTestRun run) => myInnerHostController.Cancel(run);

        public void Abort(IUnitTestRun run) => myInnerHostController.Abort(run);

        public IPreparedProcess StartProcess(ProcessStartInfo startInfo, IUnitTestRun run, ILogger logger) =>
            myInnerHostController.StartProcess(startInfo, run, logger);

        public void CustomizeConfiguration(IUnitTestRun run, TaskExecutorConfiguration configuration) =>
            myInnerHostController.CustomizeConfiguration(run, configuration);

        public async Task PrepareForRun(IUnitTestRun run)
        {
            await myInnerHostController.PrepareForRun(run).ConfigureAwait(false);

            var unityEditorProcessId = myUnityController.TryGetUnityProcessId();
            if (unityEditorProcessId.HasValue)
                return;
            
            var needStart = MessageBox.ShowYesNo("Unity hasn't started yet. Run it?", "Unity plugin");
            if (!needStart)
                throw new Exception(NotAvailableUnityEditorMessage);

            StartUnity();

            await myUnityController.WaitConnectedUnityProcessId();
        }
        
        private void StartUnity()
        {
            var commandLines = myUnityController.GetUnityCommandline();
            var unityPath = commandLines.First();
            var unityArgs = string.Join(" ", commandLines.Skip(1));
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(unityPath, unityArgs)
            };
            
            process.Start();
        }
    }
}