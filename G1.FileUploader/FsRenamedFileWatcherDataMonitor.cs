using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;

namespace FileUploader
{
	public delegate void FilesReceivedEventHandler(string[] filenames);
	
	public class FsRenamedFileWatcherDataMonitor : IObserver<EventPattern<RenamedEventArgs>>, IDisposable
	{
		private object _startStopSync = new object();
		private FileSystemWatcher _watcher;
		private IObservable<EventPattern<RenamedEventArgs>> _fileEvents;
		private RenamedEventHandler _fileEventsInternalHandler;
		private IDisposable _eventSubscription;

		private int _queueCount = 0;
		private readonly object _queueCountSync = new object();

		public event FilesReceivedEventHandler FilesReceived;

		public FsRenamedFileWatcherDataMonitor(string location)
		{
			Location = location;
			_watcher = new FileSystemWatcher(Location);
			_watcher.IncludeSubdirectories = false;
			_watcher.InternalBufferSize = 64 * 1024; //Use maximum allowed buffer size
		}

		public void Dispose()
		{
			Stop();
			_watcher.Dispose();
		}

		private void IncreaseQueueLength()
        {
			lock (_queueCountSync)
				_queueCount += 1;
        }

		private void DecreaseQueueLength()
		{
			lock (_queueCountSync)
				_queueCount -= 1;
		}

		private void ProcessFile(string filename)
		{
			if (!File.Exists(filename) || FilesReceived is null)
				return;

			try
			{
				FilesReceived(new[] { filename });
			}
			catch
			{
				// todo: Handle file processing error here or in IObserver<EventPattern<RenamedEventArgs>>.OnError(Exception) below
			}
		}

		/// <summary>
		/// Handles new (renamed) file event
		/// </summary>
		/// <param name="value"></param>
		void IObserver<EventPattern<RenamedEventArgs>>.OnNext(EventPattern<RenamedEventArgs> value)
		{
			ProcessFile(value.EventArgs.FullPath);

			DecreaseQueueLength();
		}

		void IObserver<EventPattern<RenamedEventArgs>>.OnError(Exception error)
		{
		}

		void IObserver<EventPattern<RenamedEventArgs>>.OnCompleted()
		{
		}

		public string Location { get; }

		public bool IsStarted { get; private set; }

		public void Start()
		{
			lock (_startStopSync)
			{
				if (IsStarted)
					return;

				// Use Reactive for event queueing mechanism to prevent FS file event buffer overflow causing missed files
				_fileEvents = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
						h =>
						{
							_fileEventsInternalHandler = h;
							_watcher.Renamed += Watcher_Renamed;
						},
						h => _watcher.Renamed -= Watcher_Renamed
					);

				// Subscribe to new (renamed) file events by making 'this' the event observer
				_eventSubscription = _fileEvents.Subscribe(this);
				
				_watcher.EnableRaisingEvents = true;
				IsStarted = true;
			}
		}

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
		{
			IncreaseQueueLength();
			
			// queue file new (renamed) file
			_fileEventsInternalHandler(sender, e);
		}

        public void Stop()
		{
			lock (_startStopSync)
			{
				if (!IsStarted)
					return;

				_watcher.EnableRaisingEvents = false;
				
				// wait until all files in the queue have been processed
				while(_queueCount > 0) 
					Thread.Sleep(500);

				_eventSubscription.Dispose();
				IsStarted = false;
			}
		}
	}
}