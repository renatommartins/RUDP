using System;
using System.Collections.Generic;
using System.Text;

using RUDP.Interfaces;

namespace RUDP
{
	public class Injector
	{
		static private Injector _instance;
		public static Injector Instance
		{
			get
			{
				if (_instance == null)
					_instance = new Injector();
				return _instance;
			}

			set
			{
				if (_instance == null)
					_instance = value;
				else
					throw new Exception("Instance is not null");
			}
		}

		private Dictionary<Type, Type> _typeDictionary = new Dictionary<Type, Type>()
		{
			
		};

		private T InjectorCreateInstance<T>()
		{
			return (T)_typeDictionary[typeof(T)].GetConstructor(new Type[]{ }).Invoke(new object[] { });
		}

		static public T CreateInstance<T>()
		{
			return Instance.InjectorCreateInstance<T>();
		}
	}
}
