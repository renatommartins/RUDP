using System;
using System.Collections.Generic;
using System.Text;

namespace RUDP
{
	public class Unsubscriber<R, T> : IDisposable
	{
		private Dictionary<R, T> observers;
		private T observer;

		public Unsubscriber(Dictionary<R, T> observers, T observer)
		{
			this.observers = observers;
			this.observer = observer;
		}

		public void Dispose()
		{
			if (observers != null && observers.ContainsValue(observer))
			{
				R key = default;

				foreach (KeyValuePair<R, T> pair in observers)
				{
					key = pair.Key;
				}

				observers.Remove(key);
			}
		}
	}
}
