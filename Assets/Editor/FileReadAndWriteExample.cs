using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class FileReadAndWriteExample : EditorWindow
{
    private int threadNum = Environment.ProcessorCount;

    private string sourceDirectory;

    private string destinationDirectory;

    class SimpleJob
    {
        private string sourcePath;
        private string destinationPath;

        public SimpleJob(string src, string dest)
        {
            sourcePath = src;
            destinationPath = dest;
        }

        public void Run()
        {
            var text = File.ReadAllText(sourcePath, Encoding.UTF8);
            File.WriteAllText(destinationPath, text);
        }
    }

    class Worker
    {
        private Queue<SimpleJob> jobQueue;

        public ManualResetEvent ManualResetEvent { get; private set; }

        public Worker()
        {
            this.jobQueue = new Queue<SimpleJob>();
            this.ManualResetEvent = new ManualResetEvent(false);
        }

        public void EnqueueJob(SimpleJob job)
        {
            jobQueue.Enqueue(job);
        }

        public void Run()
        {
            while (jobQueue.Count > 0)
            {
                var job = jobQueue.Dequeue();
                job.Run();
            }

            ManualResetEvent.Set();
        }
    }

    [MenuItem("Window/MultiThreadExample/SimpleIO")]
    static void Init()
    {
        GetWindow(typeof(FileReadAndWriteExample)).Show();
    }

    void OnGUI()
    {
        threadNum = EditorGUILayout.IntField("Source Directory", threadNum);
        sourceDirectory = EditorGUILayout.TextField("Source Directory", sourceDirectory);
        destinationDirectory = EditorGUILayout.TextField("Destination Directory", destinationDirectory);

        if (GUILayout.Button("Run"))
        {
            Setup();
            RunJob();
        }
    }

    void Setup()
    {
        if (string.IsNullOrEmpty(sourceDirectory) || string.IsNullOrEmpty(destinationDirectory))
        {
            return;
        }

        if (!Directory.Exists(sourceDirectory))
        {
            throw new Exception("sourceDirectory does not Exits.");
        }

        if (Directory.Exists(destinationDirectory))
        {
            Directory.Delete(destinationDirectory, true);
        }

        Directory.CreateDirectory(destinationDirectory);
    }

    void RunJob()
    {
        var workers = CreateWorkers(threadNum);
        ThreadPool.SetMaxThreads(threadNum, threadNum);
        var waitHandles = new WaitHandle[threadNum];
        var sw = new Stopwatch();
        sw.Start();
        for (var i=0; i < workers.Length; i++)
        {
            var worker = workers[i];
            ThreadPool.QueueUserWorkItem(ThreadProc, worker);
            waitHandles[i] = worker.ManualResetEvent;
        }

        WaitHandle.WaitAll(waitHandles);
        sw.Stop();
        Debug.Log(sw.ElapsedMilliseconds);
    }

    private void ThreadProc(object workerObj)
    {
        var worker = (Worker) workerObj;
        worker.Run();
        return;
    }

    Worker[] CreateWorkers(int num)
    {
        var workers = new Worker[num];
        for (var i =0; i < num; i++)
        {
            workers[i] = new Worker();
        }

        Debug.Log("workers.Lenght " + workers.Length);

        int wokerNum = 0;
        foreach (string path in Directory.GetFiles(sourceDirectory, "*"))
        {
            var fileName = Path.GetFileName(path);
            var simpleJob = new SimpleJob(path, Path.Combine(destinationDirectory, fileName));
            workers[wokerNum].EnqueueJob(simpleJob);
            wokerNum++;
            wokerNum %= num;
        }
        return workers;
    }
}
