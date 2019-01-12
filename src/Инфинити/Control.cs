using Accessibility;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Internal;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Windows.Forms.Internal;
using System.Windows.Forms.Layout;

namespace System.Windows.Forms
{
	/// <summary>Определяет базовый класс для элементов управления, являющихся компонентами с визуальным представлением.</summary>
	/// <filterpriority>1</filterpriority>
	[DefaultEvent("Click"), DefaultProperty("Text"), DesignerSerializer("System.Windows.Forms.Design.ControlCodeDomSerializer, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.ComponentModel.Design.Serialization.CodeDomSerializer, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), Designer("System.Windows.Forms.Design.ControlDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"), ToolboxItemFilter("System.Windows.Forms"), ClassInterface(ClassInterfaceType.AutoDispatch), ComVisible(true)]
	public class Control : Component, UnsafeNativeMethods.IOleControl, UnsafeNativeMethods.IOleObject, UnsafeNativeMethods.IOleInPlaceObject, UnsafeNativeMethods.IOleInPlaceActiveObject, UnsafeNativeMethods.IOleWindow, UnsafeNativeMethods.IViewObject, UnsafeNativeMethods.IViewObject2, UnsafeNativeMethods.IPersist, UnsafeNativeMethods.IPersistStreamInit, UnsafeNativeMethods.IPersistPropertyBag, UnsafeNativeMethods.IPersistStorage, UnsafeNativeMethods.IQuickActivate, ISupportOleDropSource, IDropTarget, ISynchronizeInvoke, IWin32Window, IArrangedElement, IComponent, IDisposable, IBindableComponent
	{
		private class ControlTabOrderHolder
		{
			internal readonly int oldOrder;

			internal readonly int newOrder;

			internal readonly Control control;

			internal ControlTabOrderHolder(int oldOrder, int newOrder, Control control)
			{
				this.oldOrder = oldOrder;
				this.newOrder = newOrder;
				this.control = control;
			}
		}

		private class ControlTabOrderComparer : IComparer
		{
			int IComparer.Compare(object x, object y)
			{
				Control.ControlTabOrderHolder controlTabOrderHolder = (Control.ControlTabOrderHolder)x;
				Control.ControlTabOrderHolder controlTabOrderHolder2 = (Control.ControlTabOrderHolder)y;
				int num = controlTabOrderHolder.newOrder - controlTabOrderHolder2.newOrder;
				if (num == 0)
				{
					num = controlTabOrderHolder.oldOrder - controlTabOrderHolder2.oldOrder;
				}
				return num;
			}
		}

		internal sealed class ControlNativeWindow : NativeWindow, IWindowTarget
		{
			private Control control;

			private GCHandle rootRef;

			internal IWindowTarget target;

			internal IWindowTarget WindowTarget
			{
				get
				{
					return this.target;
				}
				set
				{
					this.target = value;
				}
			}

			internal ControlNativeWindow(Control control)
			{
				this.control = control;
				this.target = this;
			}

			internal Control GetControl()
			{
				return this.control;
			}

			protected override void OnHandleChange()
			{
				this.target.OnHandleChange(base.Handle);
			}

			public void OnHandleChange(IntPtr newHandle)
			{
				this.control.SetHandle(newHandle);
			}

			internal void LockReference(bool locked)
			{
				if (locked)
				{
					if (!this.rootRef.IsAllocated)
					{
						this.rootRef = GCHandle.Alloc(this.GetControl(), GCHandleType.Normal);
						return;
					}
				}
				else if (this.rootRef.IsAllocated)
				{
					this.rootRef.Free();
				}
			}

			protected override void OnThreadException(Exception e)
			{
				this.control.WndProcException(e);
			}

			public void OnMessage(ref Message m)
			{
				this.control.WndProc(ref m);
			}

			protected override void WndProc(ref Message m)
			{
				int msg = m.Msg;
				if (msg != 512)
				{
					if (msg != 522)
					{
						if (msg == 675)
						{
							this.control.UnhookMouseEvent();
						}
					}
					else
					{
						this.control.ResetMouseEventArgs();
					}
				}
				else if (!this.control.GetState(16384))
				{
					this.control.HookMouseEvent();
					if (!this.control.GetState(8192))
					{
						this.control.SendMessage(NativeMethods.WM_MOUSEENTER, 0, 0);
					}
					else
					{
						this.control.SetState(8192, false);
					}
				}
				this.target.OnMessage(ref m);
			}
		}

		/// <summary>Представляет коллекцию объектов <see cref="T:System.Windows.Forms.Control" />.</summary>
		[ListBindable(false), ComVisible(false)]
		public class ControlCollection : ArrangedElementCollection, IList, ICollection, IEnumerable, ICloneable
		{
			private class ControlCollectionEnumerator : IEnumerator
			{
				private Control.ControlCollection controls;

				private int current;

				private int originalCount;

				public object Current
				{
					get
					{
						if (this.current == -1)
						{
							return null;
						}
						return this.controls[this.current];
					}
				}

				public ControlCollectionEnumerator(Control.ControlCollection controls)
				{
					this.controls = controls;
					this.originalCount = controls.Count;
					this.current = -1;
				}

				public bool MoveNext()
				{
					if (this.current < this.controls.Count - 1 && this.current < this.originalCount - 1)
					{
						this.current++;
						return true;
					}
					return false;
				}

				public void Reset()
				{
					this.current = -1;
				}
			}

			private Control owner;

			private int lastAccessedIndex = -1;

			/// <summary>Получает элемент управления, владеющий данной коллекцией <see cref="T:System.Windows.Forms.Control.ControlCollection" />.</summary>
			/// <returns>The <see cref="T:System.Windows.Forms.Control" /> that owns this <see cref="T:System.Windows.Forms.Control.ControlCollection" />.</returns>
			public Control Owner
			{
				get
				{
					return this.owner;
				}
			}

			/// <summary>Указывает объект <see cref="T:System.Windows.Forms.Control" />, находящийся в заданном индексом местоположении в коллекции.</summary>
			/// <returns>
			///   <see cref="T:System.Windows.Forms.Control" />, расположенный по указанному индексу в коллекции элементов управления.</returns>
			/// <param name="index">Индекс элемента управления, извлекаемого из коллекции элементов управления. </param>
			/// <exception cref="T:System.ArgumentOutOfRangeException">Значение <paramref name="index" /> меньше нуля либо больше или равно числу элементов управления в коллекции. </exception>
			public new virtual Control this[int index]
			{
				get
				{
					if (index < 0 || index >= this.Count)
					{
						throw new ArgumentOutOfRangeException("index", SR.GetString("IndexOutOfRange", new object[]
						{
							index.ToString(CultureInfo.CurrentCulture)
						}));
					}
					return (Control)base.InnerList[index];
				}
			}

			/// <summary>Указывает объект <see cref="T:System.Windows.Forms.Control" /> с заданным ключом в коллекции.</summary>
			/// <returns>Объект <see cref="T:System.Windows.Forms.Control" /> с указанным ключом в коллекции <see cref="T:System.Windows.Forms.Control.ControlCollection" />.</returns>
			/// <param name="key">Имя элемента управления, извлекаемого из коллекции элементов управления.</param>
			public virtual Control this[string key]
			{
				get
				{
					if (string.IsNullOrEmpty(key))
					{
						return null;
					}
					int index = this.IndexOfKey(key);
					if (this.IsValidIndex(index))
					{
						return this[index];
					}
					return null;
				}
			}

			/// <summary>Инициализирует новый экземпляр класса <see cref="T:System.Windows.Forms.Control.ControlCollection" />.</summary>
			/// <param name="owner">Объект <see cref="T:System.Windows.Forms.Control" />, представляющий элемент управления, которому принадлежит коллекция элементов управления. </param>
			public ControlCollection(Control owner)
			{
				this.owner = owner;
			}

			/// <summary>Определяет, содержится ли элемент с указанным ключом в коллекции <see cref="T:System.Windows.Forms.Control.ControlCollection" />.</summary>
			/// <returns>Значение true, если коллекция <see cref="T:System.Windows.Forms.Control.ControlCollection" /> содержит элемент с указанным ключом; в противном случае — значение false.</returns>
			/// <param name="key">Ключ, расположение которого в <see cref="T:System.Windows.Forms.Control.ControlCollection" /> необходимо определить. </param>
			public virtual bool ContainsKey(string key)
			{
				return this.IsValidIndex(this.IndexOfKey(key));
			}

			/// <summary>Добавляет указанный элемент управления в коллекцию элементов управления.</summary>
			/// <param name="value">Объект <see cref="T:System.Windows.Forms.Control" />, добавляемый в коллекцию элементов управления. </param>
			/// <exception cref="T:System.Exception">Указанный элемент управления — элемент управления верхнего уровня, или появляется циклическая ссылка на элемент управления, если этот элемент управления был добавлен в коллекцию элементов управления. </exception>
			/// <exception cref="T:System.ArgumentException">Объект, присвоенный параметру <paramref name="value" />, не представляет собой элемент управления <see cref="T:System.Windows.Forms.Control" />. </exception>
			public virtual void Add(Control value)
			{
				if (value == null)
				{
					return;
				}
				if (value.GetTopLevel())
				{
					throw new ArgumentException(SR.GetString("TopLevelControlAdd"));
				}
				if (this.owner.CreateThreadId != value.CreateThreadId)
				{
					throw new ArgumentException(SR.GetString("AddDifferentThreads"));
				}
				Control.CheckParentingCycle(this.owner, value);
				if (value.parent == this.owner)
				{
					value.SendToBack();
					return;
				}
				if (value.parent != null)
				{
					value.parent.Controls.Remove(value);
				}
				base.InnerList.Add(value);
				if (value.tabIndex == -1)
				{
					int num = 0;
					for (int i = 0; i < this.Count - 1; i++)
					{
						int tabIndex = this[i].TabIndex;
						if (num <= tabIndex)
						{
							num = tabIndex + 1;
						}
					}
					value.tabIndex = num;
				}
				this.owner.SuspendLayout();
				try
				{
					Control parent = value.parent;
					try
					{
						value.AssignParent(this.owner);
					}
					finally
					{
						if (parent != value.parent && (this.owner.state & 1) != 0)
						{
							value.SetParentHandle(this.owner.InternalHandle);
							if (value.Visible)
							{
								value.CreateControl();
							}
						}
					}
					value.InitLayout();
				}
				finally
				{
					this.owner.ResumeLayout(false);
				}
				LayoutTransaction.DoLayout(this.owner, value, PropertyNames.Parent);
				this.owner.OnControlAdded(new ControlEventArgs(value));
			}

			/// <summary>Описание этого элемента см. в <see cref="M:System.Collections.IList.Add(System.Object)" />.</summary>
			int IList.Add(object control)
			{
				if (control is Control)
				{
					this.Add((Control)control);
					return this.IndexOf((Control)control);
				}
				throw new ArgumentException(SR.GetString("ControlBadControl"), "control");
			}

			/// <summary>Добавляет массив объектов управления в коллекцию.</summary>
			/// <param name="controls">Массив объектов <see cref="T:System.Windows.Forms.Control" />, добавляемый в коллекцию. </param>
			[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
			public virtual void AddRange(Control[] controls)
			{
				if (controls == null)
				{
					throw new ArgumentNullException("controls");
				}
				if (controls.Length != 0)
				{
					this.owner.SuspendLayout();
					try
					{
						for (int i = 0; i < controls.Length; i++)
						{
							this.Add(controls[i]);
						}
					}
					finally
					{
						this.owner.ResumeLayout(true);
					}
				}
			}

			/// <summary>Описание этого элемента см. в <see cref="M:System.ICloneable.Clone" />.</summary>
			object ICloneable.Clone()
			{
				Control.ControlCollection expr_0B = this.owner.CreateControlsInstance();
				expr_0B.InnerList.AddRange(this);
				return expr_0B;
			}

			/// <summary>Определяет, является ли указанный элемент управления членом коллекции.</summary>
			/// <returns>Значение true, если объект <see cref="T:System.Windows.Forms.Control" /> является членом коллекции; в противном случае — значение false.</returns>
			/// <param name="control">Объект <see cref="T:System.Windows.Forms.Control" /> для поиска в коллекции. </param>
			public bool Contains(Control control)
			{
				return base.InnerList.Contains(control);
			}

			/// <summary>Выполняет поиск элементов управления по их свойству <see cref="P:System.Windows.Forms.Control.Name" /> и создает массив из всех элементов управления, которые соответствуют условиям поиска.</summary>
			/// <returns>Массив типа <see cref="T:System.Windows.Forms.Control" />, содержащий совпадающие элементы управления.</returns>
			/// <param name="key">Ключ, расположение которого в <see cref="T:System.Windows.Forms.Control.ControlCollection" /> необходимо определить. </param>
			/// <param name="searchAllChildren">Значение true, если требуется найти все дочерние элементы управления; в противном случае — значение false. </param>
			/// <exception cref="T:System.ArgumentException">Значение параметра <paramref name="key" /> — null или пустая строка (""). </exception>
			public Control[] Find(string key, bool searchAllChildren)
			{
				if (string.IsNullOrEmpty(key))
				{
					throw new ArgumentNullException("key", SR.GetString("FindKeyMayNotBeEmptyOrNull"));
				}
				ArrayList expr_2B = this.FindInternal(key, searchAllChildren, this, new ArrayList());
				Control[] array = new Control[expr_2B.Count];
				expr_2B.CopyTo(array, 0);
				return array;
			}

			private ArrayList FindInternal(string key, bool searchAllChildren, Control.ControlCollection controlsToLookIn, ArrayList foundControls)
			{
				if (controlsToLookIn == null || foundControls == null)
				{
					return null;
				}
				try
				{
					for (int i = 0; i < controlsToLookIn.Count; i++)
					{
						if (controlsToLookIn[i] != null && WindowsFormsUtils.SafeCompareStrings(controlsToLookIn[i].Name, key, true))
						{
							foundControls.Add(controlsToLookIn[i]);
						}
					}
					if (searchAllChildren)
					{
						for (int j = 0; j < controlsToLookIn.Count; j++)
						{
							if (controlsToLookIn[j] != null && controlsToLookIn[j].Controls != null && controlsToLookIn[j].Controls.Count > 0)
							{
								foundControls = this.FindInternal(key, searchAllChildren, controlsToLookIn[j].Controls, foundControls);
							}
						}
					}
				}
				catch (Exception arg_A1_0)
				{
					if (ClientUtils.IsSecurityOrCriticalException(arg_A1_0))
					{
						throw;
					}
				}
				return foundControls;
			}

			/// <summary>Извлекает ссылку на объект перечислителя, который используется для итерации по коллекции <see cref="T:System.Windows.Forms.Control.ControlCollection" />.</summary>
			/// <returns>Объект <see cref="T:System.Collections.IEnumerator" />.</returns>
			public override IEnumerator GetEnumerator()
			{
				return new Control.ControlCollection.ControlCollectionEnumerator(this);
			}

			/// <summary>Извлекает индекс указанного элемента управления в коллекции элементов управления.</summary>
			/// <returns>Значение индекса, отсчитываемого с нуля, который представляет положение указанного объекта <see cref="T:System.Windows.Forms.Control" /> в коллекции <see cref="T:System.Windows.Forms.Control.ControlCollection" />.</returns>
			/// <param name="control">Объект <see cref="T:System.Windows.Forms.Control" /> для поиска в коллекции. </param>
			public int IndexOf(Control control)
			{
				return base.InnerList.IndexOf(control);
			}

			/// <summary>Извлекает индекс первого вхождения заданного элемента в коллекции.</summary>
			/// <returns>Индекс (отсчитываемый с нуля) первого вхождения элемента управления с указанным именем в коллекции.</returns>
			/// <param name="key">Имя искомого элемента управления. </param>
			public virtual int IndexOfKey(string key)
			{
				if (string.IsNullOrEmpty(key))
				{
					return -1;
				}
				if (this.IsValidIndex(this.lastAccessedIndex) && WindowsFormsUtils.SafeCompareStrings(this[this.lastAccessedIndex].Name, key, true))
				{
					return this.lastAccessedIndex;
				}
				for (int i = 0; i < this.Count; i++)
				{
					if (WindowsFormsUtils.SafeCompareStrings(this[i].Name, key, true))
					{
						this.lastAccessedIndex = i;
						return i;
					}
				}
				this.lastAccessedIndex = -1;
				return -1;
			}

			private bool IsValidIndex(int index)
			{
				return index >= 0 && index < this.Count;
			}

			/// <summary>Удаляет указанный элемент управления из коллекции.</summary>
			/// <param name="value">Объект <see cref="T:System.Windows.Forms.Control" />, удаляемый из коллекции <see cref="T:System.Windows.Forms.Control.ControlCollection" />. </param>
			public virtual void Remove(Control value)
			{
				if (value == null)
				{
					return;
				}
				if (value.ParentInternal == this.owner)
				{
					value.SetParentHandle(IntPtr.Zero);
					base.InnerList.Remove(value);
					value.AssignParent(null);
					LayoutTransaction.DoLayout(this.owner, value, PropertyNames.Parent);
					this.owner.OnControlRemoved(new ControlEventArgs(value));
					ContainerControl containerControl = this.owner.GetContainerControlInternal() as ContainerControl;
					if (containerControl != null)
					{
						containerControl.AfterControlRemoved(value, this.owner);
					}
				}
			}

			/// <summary>Описание этого элемента см. в <see cref="M:System.Collections.IList.Remove(System.Object)" />.</summary>
			void IList.Remove(object control)
			{
				if (control is Control)
				{
					this.Remove((Control)control);
				}
			}

			/// <summary>Удаляет элемент управления из коллекции по указанному расположению индекса.</summary>
			/// <param name="index">Значение индекса удаляемого объекта <see cref="T:System.Windows.Forms.Control" />. </param>
			public void RemoveAt(int index)
			{
				this.Remove(this[index]);
			}

			/// <summary>Удаляет дочерний элемент управления с указанным ключом.</summary>
			/// <param name="key">Имя удаляемого дочернего элемента управления. </param>
			public virtual void RemoveByKey(string key)
			{
				int index = this.IndexOfKey(key);
				if (this.IsValidIndex(index))
				{
					this.RemoveAt(index);
				}
			}

			/// <summary>Удаляет все элементы управления из коллекции.</summary>
			public virtual void Clear()
			{
				this.owner.SuspendLayout();
				CommonProperties.xClearAllPreferredSizeCaches(this.owner);
				try
				{
					while (this.Count != 0)
					{
						this.RemoveAt(this.Count - 1);
					}
				}
				finally
				{
					this.owner.ResumeLayout();
				}
			}

			/// <summary>Извлекает индекс указанного дочернего элемента управления в коллекции элементов управления.</summary>
			/// <returns>Значение индекса, отсчитываемого с нуля, который представляет место указанного дочернего элемента управления в коллекции элементов управления.</returns>
			/// <param name="child">Объект <see cref="T:System.Windows.Forms.Control" />, который нужно найти в коллекции элементов управления. </param>
			/// <exception cref="T:System.ArgumentException">The <paramref name="child" /><see cref="T:System.Windows.Forms.Control" /> is not in the <see cref="T:System.Windows.Forms.Control.ControlCollection" />. </exception>
			public int GetChildIndex(Control child)
			{
				return this.GetChildIndex(child, true);
			}

			/// <summary>Извлекает индекс указанного дочернего элемента управления в коллекции и при необходимости вызывает исключение, если указанный элемент управления не обнаружен в коллекции элементов управления.</summary>
			/// <returns>Значение индекса, отсчитываемого с нуля, который представляет место указанного дочернего элемента управления в коллекции элементов управления; или -1, если указанный <see cref="T:System.Windows.Forms.Control" /> не обнаружен в коллекции <see cref="T:System.Windows.Forms.Control.ControlCollection" />.</returns>
			/// <param name="child">Объект <see cref="T:System.Windows.Forms.Control" />, который нужно найти в коллекции элементов управления. </param>
			/// <param name="throwException">Значение true — для создания исключения, если <see cref="T:System.Windows.Forms.Control" />, указанный в параметре <paramref name="child" />, не является элементом управления в коллекции <see cref="T:System.Windows.Forms.Control.ControlCollection" />; в противном случае — значение false. </param>
			/// <exception cref="T:System.ArgumentException">The <paramref name="child" /><see cref="T:System.Windows.Forms.Control" /> is not in the <see cref="T:System.Windows.Forms.Control.ControlCollection" />, and the <paramref name="throwException" /> parameter value is true. </exception>
			public virtual int GetChildIndex(Control child, bool throwException)
			{
				int expr_07 = this.IndexOf(child);
				if (expr_07 == -1 & throwException)
				{
					throw new ArgumentException(SR.GetString("ControlNotChild"));
				}
				return expr_07;
			}

			internal virtual void SetChildIndexInternal(Control child, int newIndex)
			{
				if (child == null)
				{
					throw new ArgumentNullException("child");
				}
				int childIndex = this.GetChildIndex(child);
				if (childIndex == newIndex)
				{
					return;
				}
				if (newIndex >= this.Count || newIndex == -1)
				{
					newIndex = this.Count - 1;
				}
				base.MoveElement(child, childIndex, newIndex);
				child.UpdateZOrder();
				LayoutTransaction.DoLayout(this.owner, child, PropertyNames.ChildIndex);
			}

			/// <summary>Задает определенное значение индексу указанного дочернего элемента управления в коллекции.</summary>
			/// <param name="child">The <paramref name="child" /><see cref="T:System.Windows.Forms.Control" /> to search for. </param>
			/// <param name="newIndex">Новое значение индекса элемента управления. </param>
			/// <exception cref="T:System.ArgumentException">Элемент управления <paramref name="child" /> отсутствует в коллекции <see cref="T:System.Windows.Forms.Control.ControlCollection" />. </exception>
			public virtual void SetChildIndex(Control child, int newIndex)
			{
				this.SetChildIndexInternal(child, newIndex);
			}
		}

		private class ActiveXImpl : MarshalByRefObject, IWindowTarget
		{
			internal static class AdviseHelper
			{
				internal class SafeIUnknown : SafeHandle
				{
					public sealed override bool IsInvalid
					{
						get
						{
							return base.IsClosed || IntPtr.Zero == this.handle;
						}
					}

					public SafeIUnknown(object obj, bool addRefIntPtr) : this(obj, addRefIntPtr, Guid.Empty)
					{
					}

					public SafeIUnknown(object obj, bool addRefIntPtr, Guid iid) : base(IntPtr.Zero, true)
					{
						RuntimeHelpers.PrepareConstrainedRegions();
						try
						{
						}
						finally
						{
							IntPtr intPtr;
							if (obj is IntPtr)
							{
								intPtr = (IntPtr)obj;
								if (addRefIntPtr)
								{
									Marshal.AddRef(intPtr);
								}
							}
							else
							{
								intPtr = Marshal.GetIUnknownForObject(obj);
							}
							if (iid != Guid.Empty)
							{
								IntPtr pUnk = intPtr;
								try
								{
									intPtr = Control.ActiveXImpl.AdviseHelper.SafeIUnknown.InternalQueryInterface(intPtr, ref iid);
								}
								finally
								{
									Marshal.Release(pUnk);
								}
							}
							this.handle = intPtr;
						}
					}

					private static IntPtr InternalQueryInterface(IntPtr pUnk, ref Guid iid)
					{
						IntPtr intPtr;
						if (Marshal.QueryInterface(pUnk, ref iid, out intPtr) != 0 || intPtr == IntPtr.Zero)
						{
							throw new InvalidCastException(SR.GetString("AxInterfaceNotSupported"));
						}
						return intPtr;
					}

					protected sealed override bool ReleaseHandle()
					{
						IntPtr handle = this.handle;
						this.handle = IntPtr.Zero;
						if (IntPtr.Zero != handle)
						{
							Marshal.Release(handle);
						}
						return true;
					}

					protected V LoadVtable<V>()
					{
						return (V)((object)Marshal.PtrToStructure(Marshal.ReadIntPtr(this.handle, 0), typeof(V)));
					}
				}

				internal sealed class ComConnectionPointContainer : Control.ActiveXImpl.AdviseHelper.SafeIUnknown
				{
					[StructLayout(LayoutKind.Sequential)]
					private class VTABLE
					{
						public IntPtr QueryInterfacePtr;

						public IntPtr AddRefPtr;

						public IntPtr ReleasePtr;

						public IntPtr EnumConnectionPointsPtr;

						public IntPtr FindConnectionPointPtr;
					}

					[UnmanagedFunctionPointer(CallingConvention.StdCall)]
					private delegate int FindConnectionPointD(IntPtr This, ref Guid iid, out IntPtr ppv);

					private Control.ActiveXImpl.AdviseHelper.ComConnectionPointContainer.VTABLE vtbl;

					public ComConnectionPointContainer(object obj, bool addRefIntPtr) : base(obj, addRefIntPtr, typeof(IConnectionPointContainer).GUID)
					{
						this.vtbl = base.LoadVtable<Control.ActiveXImpl.AdviseHelper.ComConnectionPointContainer.VTABLE>();
					}

					public Control.ActiveXImpl.AdviseHelper.ComConnectionPoint FindConnectionPoint(Type eventInterface)
					{
						Control.ActiveXImpl.AdviseHelper.ComConnectionPointContainer.FindConnectionPointD arg_36_0 = (Control.ActiveXImpl.AdviseHelper.ComConnectionPointContainer.FindConnectionPointD)Marshal.GetDelegateForFunctionPointer(this.vtbl.FindConnectionPointPtr, typeof(Control.ActiveXImpl.AdviseHelper.ComConnectionPointContainer.FindConnectionPointD));
						IntPtr zero = IntPtr.Zero;
						Guid gUID = eventInterface.GUID;
						if (arg_36_0(this.handle, ref gUID, out zero) != 0 || zero == IntPtr.Zero)
						{
							throw new ArgumentException(SR.GetString("AXNoConnectionPoint", new object[]
							{
								eventInterface.Name
							}));
						}
						return new Control.ActiveXImpl.AdviseHelper.ComConnectionPoint(zero, false);
					}
				}

				internal sealed class ComConnectionPoint : Control.ActiveXImpl.AdviseHelper.SafeIUnknown
				{
					[StructLayout(LayoutKind.Sequential)]
					private class VTABLE
					{
						public IntPtr QueryInterfacePtr;

						public IntPtr AddRefPtr;

						public IntPtr ReleasePtr;

						public IntPtr GetConnectionInterfacePtr;

						public IntPtr GetConnectionPointContainterPtr;

						public IntPtr AdvisePtr;

						public IntPtr UnadvisePtr;

						public IntPtr EnumConnectionsPtr;
					}

					[UnmanagedFunctionPointer(CallingConvention.StdCall)]
					private delegate int AdviseD(IntPtr This, IntPtr punkEventSink, out int cookie);

					private Control.ActiveXImpl.AdviseHelper.ComConnectionPoint.VTABLE vtbl;

					public ComConnectionPoint(object obj, bool addRefIntPtr) : base(obj, addRefIntPtr, typeof(IConnectionPoint).GUID)
					{
						this.vtbl = base.LoadVtable<Control.ActiveXImpl.AdviseHelper.ComConnectionPoint.VTABLE>();
					}

					public bool Advise(IntPtr punkEventSink, out int cookie)
					{
						return ((Control.ActiveXImpl.AdviseHelper.ComConnectionPoint.AdviseD)Marshal.GetDelegateForFunctionPointer(this.vtbl.AdvisePtr, typeof(Control.ActiveXImpl.AdviseHelper.ComConnectionPoint.AdviseD)))(this.handle, punkEventSink, out cookie) == 0;
					}
				}

				public static bool AdviseConnectionPoint(object connectionPoint, object sink, Type eventInterface, out int cookie)
				{
					bool result;
					using (Control.ActiveXImpl.AdviseHelper.ComConnectionPointContainer comConnectionPointContainer = new Control.ActiveXImpl.AdviseHelper.ComConnectionPointContainer(connectionPoint, true))
					{
						result = Control.ActiveXImpl.AdviseHelper.AdviseConnectionPoint(comConnectionPointContainer, sink, eventInterface, out cookie);
					}
					return result;
				}

				internal static bool AdviseConnectionPoint(Control.ActiveXImpl.AdviseHelper.ComConnectionPointContainer cpc, object sink, Type eventInterface, out int cookie)
				{
					bool result;
					using (Control.ActiveXImpl.AdviseHelper.ComConnectionPoint comConnectionPoint = cpc.FindConnectionPoint(eventInterface))
					{
						using (Control.ActiveXImpl.AdviseHelper.SafeIUnknown safeIUnknown = new Control.ActiveXImpl.AdviseHelper.SafeIUnknown(sink, true))
						{
							result = comConnectionPoint.Advise(safeIUnknown.DangerousGetHandle(), out cookie);
						}
					}
					return result;
				}
			}

			private class PropertyBagStream : UnsafeNativeMethods.IPropertyBag
			{
				private Hashtable bag = new Hashtable();

				[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
				internal void Read(UnsafeNativeMethods.IStream istream)
				{
					Stream stream = new DataStreamFromComStream(istream);
					byte[] array = new byte[4096];
					int num = 0;
					int num2 = stream.Read(array, num, 4096);
					int num3 = num2;
					while (num2 == 4096)
					{
						byte[] array2 = new byte[array.Length + 4096];
						Array.Copy(array, array2, array.Length);
						array = array2;
						num += 4096;
						num2 = stream.Read(array, num, 4096);
						num3 += num2;
					}
					stream = new MemoryStream(array);
					BinaryFormatter binaryFormatter = new BinaryFormatter();
					try
					{
						this.bag = (Hashtable)binaryFormatter.Deserialize(stream);
					}
					catch (Exception arg_8C_0)
					{
						if (ClientUtils.IsSecurityOrCriticalException(arg_8C_0))
						{
							throw;
						}
						this.bag = new Hashtable();
					}
				}

				[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
				int UnsafeNativeMethods.IPropertyBag.Read(string pszPropName, ref object pVar, UnsafeNativeMethods.IErrorLog pErrorLog)
				{
					if (!this.bag.Contains(pszPropName))
					{
						return -2147024809;
					}
					pVar = this.bag[pszPropName];
					return 0;
				}

				[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
				int UnsafeNativeMethods.IPropertyBag.Write(string pszPropName, ref object pVar)
				{
					this.bag[pszPropName] = pVar;
					return 0;
				}

				[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
				internal void Write(UnsafeNativeMethods.IStream istream)
				{
					Stream serializationStream = new DataStreamFromComStream(istream);
					new BinaryFormatter().Serialize(serializationStream, this.bag);
				}
			}

			private static readonly int hiMetricPerInch = 2540;

			private static readonly int viewAdviseOnlyOnce = BitVector32.CreateMask();

			private static readonly int viewAdvisePrimeFirst = BitVector32.CreateMask(Control.ActiveXImpl.viewAdviseOnlyOnce);

			private static readonly int eventsFrozen = BitVector32.CreateMask(Control.ActiveXImpl.viewAdvisePrimeFirst);

			private static readonly int changingExtents = BitVector32.CreateMask(Control.ActiveXImpl.eventsFrozen);

			private static readonly int saving = BitVector32.CreateMask(Control.ActiveXImpl.changingExtents);

			private static readonly int isDirty = BitVector32.CreateMask(Control.ActiveXImpl.saving);

			private static readonly int inPlaceActive = BitVector32.CreateMask(Control.ActiveXImpl.isDirty);

			private static readonly int inPlaceVisible = BitVector32.CreateMask(Control.ActiveXImpl.inPlaceActive);

			private static readonly int uiActive = BitVector32.CreateMask(Control.ActiveXImpl.inPlaceVisible);

			private static readonly int uiDead = BitVector32.CreateMask(Control.ActiveXImpl.uiActive);

			private static readonly int adjustingRect = BitVector32.CreateMask(Control.ActiveXImpl.uiDead);

			private static Point logPixels = Point.Empty;

			private static NativeMethods.tagOLEVERB[] axVerbs;

			private static int globalActiveXCount = 0;

			private static bool checkedIE;

			private static bool isIE;

			private Control control;

			private IWindowTarget controlWindowTarget;

			private IntPtr clipRegion;

			private UnsafeNativeMethods.IOleClientSite clientSite;

			private UnsafeNativeMethods.IOleInPlaceUIWindow inPlaceUiWindow;

			private UnsafeNativeMethods.IOleInPlaceFrame inPlaceFrame;

			private ArrayList adviseList;

			private IAdviseSink viewAdviseSink;

			private BitVector32 activeXState;

			private Control.AmbientProperty[] ambientProperties;

			private IntPtr hwndParent;

			private IntPtr accelTable;

			private short accelCount = -1;

			private NativeMethods.COMRECT adjustRect;

			[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced)]
			internal Color AmbientBackColor
			{
				get
				{
					Control.AmbientProperty ambientProperty = this.LookupAmbient(-701);
					if (ambientProperty.Empty)
					{
						object obj = null;
						if (this.GetAmbientProperty(-701, ref obj) && obj != null)
						{
							try
							{
								ambientProperty.Value = ColorTranslator.FromOle(Convert.ToInt32(obj, CultureInfo.InvariantCulture));
							}
							catch (Exception arg_45_0)
							{
								if (ClientUtils.IsSecurityOrCriticalException(arg_45_0))
								{
									throw;
								}
							}
						}
					}
					if (ambientProperty.Value == null)
					{
						return Color.Empty;
					}
					return (Color)ambientProperty.Value;
				}
			}

			[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced)]
			internal Font AmbientFont
			{
				get
				{
					Control.AmbientProperty ambientProperty = this.LookupAmbient(-703);
					if (ambientProperty.Empty)
					{
						object obj = null;
						if (this.GetAmbientProperty(-703, ref obj))
						{
							try
							{
								IntPtr arg_2A_0 = IntPtr.Zero;
								UnsafeNativeMethods.IFont font = (UnsafeNativeMethods.IFont)obj;
								IntSecurity.ObjectFromWin32Handle.Assert();
								Font value = null;
								try
								{
									value = Font.FromHfont(font.GetHFont());
								}
								finally
								{
									CodeAccessPermission.RevertAssert();
								}
								ambientProperty.Value = value;
							}
							catch (Exception arg_5B_0)
							{
								if (ClientUtils.IsSecurityOrCriticalException(arg_5B_0))
								{
									throw;
								}
								ambientProperty.Value = null;
							}
						}
					}
					return (Font)ambientProperty.Value;
				}
			}

			[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced)]
			internal Color AmbientForeColor
			{
				get
				{
					Control.AmbientProperty ambientProperty = this.LookupAmbient(-704);
					if (ambientProperty.Empty)
					{
						object obj = null;
						if (this.GetAmbientProperty(-704, ref obj) && obj != null)
						{
							try
							{
								ambientProperty.Value = ColorTranslator.FromOle(Convert.ToInt32(obj, CultureInfo.InvariantCulture));
							}
							catch (Exception arg_45_0)
							{
								if (ClientUtils.IsSecurityOrCriticalException(arg_45_0))
								{
									throw;
								}
							}
						}
					}
					if (ambientProperty.Value == null)
					{
						return Color.Empty;
					}
					return (Color)ambientProperty.Value;
				}
			}

			[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced)]
			internal bool EventsFrozen
			{
				get
				{
					return this.activeXState[Control.ActiveXImpl.eventsFrozen];
				}
				set
				{
					this.activeXState[Control.ActiveXImpl.eventsFrozen] = value;
				}
			}

			internal IntPtr HWNDParent
			{
				get
				{
					return this.hwndParent;
				}
			}

			internal bool IsIE
			{
				[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
				get
				{
					if (!Control.ActiveXImpl.checkedIE)
					{
						if (this.clientSite == null)
						{
							return false;
						}
						if (Assembly.GetEntryAssembly() == null)
						{
							UnsafeNativeMethods.IOleContainer oleContainer;
							if (NativeMethods.Succeeded(this.clientSite.GetContainer(out oleContainer)) && oleContainer is NativeMethods.IHTMLDocument)
							{
								Control.ActiveXImpl.isIE = true;
							}
							if (oleContainer != null && UnsafeNativeMethods.IsComObject(oleContainer))
							{
								UnsafeNativeMethods.ReleaseComObject(oleContainer);
							}
						}
						Control.ActiveXImpl.checkedIE = true;
					}
					return Control.ActiveXImpl.isIE;
				}
			}

			private Point LogPixels
			{
				get
				{
					if (Control.ActiveXImpl.logPixels.IsEmpty)
					{
						Control.ActiveXImpl.logPixels = default(Point);
						IntPtr dC = UnsafeNativeMethods.GetDC(NativeMethods.NullHandleRef);
						Control.ActiveXImpl.logPixels.X = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(null, dC), 88);
						Control.ActiveXImpl.logPixels.Y = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(null, dC), 90);
						UnsafeNativeMethods.ReleaseDC(NativeMethods.NullHandleRef, new HandleRef(null, dC));
					}
					return Control.ActiveXImpl.logPixels;
				}
			}

			internal ActiveXImpl(Control control)
			{
				this.control = control;
				this.controlWindowTarget = control.WindowTarget;
				control.WindowTarget = this;
				this.adviseList = new ArrayList();
				this.activeXState = default(BitVector32);
				this.ambientProperties = new Control.AmbientProperty[]
				{
					new Control.AmbientProperty("Font", -703),
					new Control.AmbientProperty("BackColor", -701),
					new Control.AmbientProperty("ForeColor", -704)
				};
			}

			internal int Advise(IAdviseSink pAdvSink)
			{
				this.adviseList.Add(pAdvSink);
				return this.adviseList.Count;
			}

			internal void Close(int dwSaveOption)
			{
				if (this.activeXState[Control.ActiveXImpl.inPlaceActive])
				{
					this.InPlaceDeactivate();
				}
				if ((dwSaveOption == 0 || dwSaveOption == 2) && this.activeXState[Control.ActiveXImpl.isDirty])
				{
					if (this.clientSite != null)
					{
						this.clientSite.SaveObject();
					}
					this.SendOnSave();
				}
			}

			internal void DoVerb(int iVerb, IntPtr lpmsg, UnsafeNativeMethods.IOleClientSite pActiveSite, int lindex, IntPtr hwndParent, NativeMethods.COMRECT lprcPosRect)
			{
				switch (iVerb)
				{
				case -5:
				case -4:
				case -1:
				case 0:
				{
					this.InPlaceActivate(iVerb);
					if (!(lpmsg != IntPtr.Zero))
					{
						return;
					}
					NativeMethods.MSG mSG = (NativeMethods.MSG)UnsafeNativeMethods.PtrToStructure(lpmsg, typeof(NativeMethods.MSG));
					Control control = this.control;
					if (mSG.hwnd != this.control.Handle && mSG.message >= 512 && mSG.message <= 522)
					{
						IntPtr handle = (mSG.hwnd == IntPtr.Zero) ? hwndParent : mSG.hwnd;
						NativeMethods.POINT pOINT = new NativeMethods.POINT();
						pOINT.x = NativeMethods.Util.LOWORD(mSG.lParam);
						pOINT.y = NativeMethods.Util.HIWORD(mSG.lParam);
						UnsafeNativeMethods.MapWindowPoints(new HandleRef(null, handle), new HandleRef(this.control, this.control.Handle), pOINT, 1);
						Control childAtPoint = control.GetChildAtPoint(new Point(pOINT.x, pOINT.y));
						if (childAtPoint != null && childAtPoint != control)
						{
							UnsafeNativeMethods.MapWindowPoints(new HandleRef(control, control.Handle), new HandleRef(childAtPoint, childAtPoint.Handle), pOINT, 1);
							control = childAtPoint;
						}
						mSG.lParam = NativeMethods.Util.MAKELPARAM(pOINT.x, pOINT.y);
					}
					if (mSG.message == 256 && mSG.wParam == (IntPtr)9)
					{
						control.SelectNextControl(null, Control.ModifierKeys != Keys.Shift, true, true, true);
						return;
					}
					control.SendMessage(mSG.message, mSG.wParam, mSG.lParam);
					return;
				}
				case -3:
					this.UIDeactivate();
					this.InPlaceDeactivate();
					if (this.activeXState[Control.ActiveXImpl.inPlaceVisible])
					{
						this.SetInPlaceVisible(false);
						return;
					}
					return;
				}
				Control.ActiveXImpl.ThrowHr(-2147467263);
			}

			internal void Draw(int dwDrawAspect, int lindex, IntPtr pvAspect, NativeMethods.tagDVTARGETDEVICE ptd, IntPtr hdcTargetDev, IntPtr hdcDraw, NativeMethods.COMRECT prcBounds, NativeMethods.COMRECT lprcWBounds, IntPtr pfnContinue, int dwContinue)
			{
				if (dwDrawAspect != 1 && dwDrawAspect != 16 && dwDrawAspect != 32)
				{
					Control.ActiveXImpl.ThrowHr(-2147221397);
				}
				int objectType = UnsafeNativeMethods.GetObjectType(new HandleRef(null, hdcDraw));
				if (objectType == 4)
				{
					Control.ActiveXImpl.ThrowHr(-2147221184);
				}
				NativeMethods.POINT pOINT = new NativeMethods.POINT();
				NativeMethods.POINT pOINT2 = new NativeMethods.POINT();
				NativeMethods.SIZE sIZE = new NativeMethods.SIZE();
				NativeMethods.SIZE sIZE2 = new NativeMethods.SIZE();
				int nMapMode = 1;
				if (!this.control.IsHandleCreated)
				{
					this.control.CreateHandle();
				}
				if (prcBounds != null)
				{
					NativeMethods.RECT rECT = new NativeMethods.RECT(prcBounds.left, prcBounds.top, prcBounds.right, prcBounds.bottom);
					SafeNativeMethods.LPtoDP(new HandleRef(null, hdcDraw), ref rECT, 2);
					nMapMode = SafeNativeMethods.SetMapMode(new HandleRef(null, hdcDraw), 8);
					SafeNativeMethods.SetWindowOrgEx(new HandleRef(null, hdcDraw), 0, 0, pOINT2);
					SafeNativeMethods.SetWindowExtEx(new HandleRef(null, hdcDraw), this.control.Width, this.control.Height, sIZE);
					SafeNativeMethods.SetViewportOrgEx(new HandleRef(null, hdcDraw), rECT.left, rECT.top, pOINT);
					SafeNativeMethods.SetViewportExtEx(new HandleRef(null, hdcDraw), rECT.right - rECT.left, rECT.bottom - rECT.top, sIZE2);
				}
				try
				{
					IntPtr intPtr = (IntPtr)30;
					if (objectType != 12)
					{
						this.control.SendMessage(791, hdcDraw, intPtr);
					}
					else
					{
						this.control.PrintToMetaFile(new HandleRef(null, hdcDraw), intPtr);
					}
				}
				finally
				{
					if (prcBounds != null)
					{
						SafeNativeMethods.SetWindowOrgEx(new HandleRef(null, hdcDraw), pOINT2.x, pOINT2.y, null);
						SafeNativeMethods.SetWindowExtEx(new HandleRef(null, hdcDraw), sIZE.cx, sIZE.cy, null);
						SafeNativeMethods.SetViewportOrgEx(new HandleRef(null, hdcDraw), pOINT.x, pOINT.y, null);
						SafeNativeMethods.SetViewportExtEx(new HandleRef(null, hdcDraw), sIZE2.cx, sIZE2.cy, null);
						SafeNativeMethods.SetMapMode(new HandleRef(null, hdcDraw), nMapMode);
					}
				}
			}

			internal static int EnumVerbs(out UnsafeNativeMethods.IEnumOLEVERB e)
			{
				if (Control.ActiveXImpl.axVerbs == null)
				{
					NativeMethods.tagOLEVERB tagOLEVERB = new NativeMethods.tagOLEVERB();
					NativeMethods.tagOLEVERB tagOLEVERB2 = new NativeMethods.tagOLEVERB();
					NativeMethods.tagOLEVERB tagOLEVERB3 = new NativeMethods.tagOLEVERB();
					NativeMethods.tagOLEVERB tagOLEVERB4 = new NativeMethods.tagOLEVERB();
					NativeMethods.tagOLEVERB tagOLEVERB5 = new NativeMethods.tagOLEVERB();
					NativeMethods.tagOLEVERB arg_55_0 = new NativeMethods.tagOLEVERB();
					tagOLEVERB.lVerb = -1;
					tagOLEVERB2.lVerb = -5;
					tagOLEVERB3.lVerb = -4;
					tagOLEVERB4.lVerb = -3;
					tagOLEVERB5.lVerb = 0;
					arg_55_0.lVerb = -7;
					arg_55_0.lpszVerbName = SR.GetString("AXProperties");
					arg_55_0.grfAttribs = 2;
					Control.ActiveXImpl.axVerbs = new NativeMethods.tagOLEVERB[]
					{
						tagOLEVERB,
						tagOLEVERB2,
						tagOLEVERB3,
						tagOLEVERB4,
						tagOLEVERB5
					};
				}
				e = new Control.ActiveXVerbEnum(Control.ActiveXImpl.axVerbs);
				return 0;
			}

			private static byte[] FromBase64WrappedString(string text)
			{
				if (text.IndexOfAny(new char[]
				{
					' ',
					'\r',
					'\n'
				}) != -1)
				{
					StringBuilder stringBuilder = new StringBuilder(text.Length);
					for (int i = 0; i < text.Length; i++)
					{
						char c = text[i];
						if (c != '\n' && c != '\r' && c != ' ')
						{
							stringBuilder.Append(text[i]);
						}
					}
					return Convert.FromBase64String(stringBuilder.ToString());
				}
				return Convert.FromBase64String(text);
			}

			internal void GetAdvise(int[] paspects, int[] padvf, IAdviseSink[] pAdvSink)
			{
				if (paspects != null)
				{
					paspects[0] = 1;
				}
				if (padvf != null)
				{
					padvf[0] = 0;
					if (this.activeXState[Control.ActiveXImpl.viewAdviseOnlyOnce])
					{
						padvf[0] |= 4;
					}
					if (this.activeXState[Control.ActiveXImpl.viewAdvisePrimeFirst])
					{
						padvf[0] |= 2;
					}
				}
				if (pAdvSink != null)
				{
					pAdvSink[0] = this.viewAdviseSink;
				}
			}

			private bool GetAmbientProperty(int dispid, ref object obj)
			{
				if (this.clientSite is UnsafeNativeMethods.IDispatch)
				{
					UnsafeNativeMethods.IDispatch dispatch = (UnsafeNativeMethods.IDispatch)this.clientSite;
					object[] array = new object[1];
					Guid empty = Guid.Empty;
					int hr = -2147467259;
					IntSecurity.UnmanagedCode.Assert();
					try
					{
						hr = dispatch.Invoke(dispid, ref empty, NativeMethods.LOCALE_USER_DEFAULT, 2, new NativeMethods.tagDISPPARAMS(), array, null, null);
					}
					finally
					{
						CodeAccessPermission.RevertAssert();
					}
					if (NativeMethods.Succeeded(hr))
					{
						obj = array[0];
						return true;
					}
				}
				return false;
			}

			internal UnsafeNativeMethods.IOleClientSite GetClientSite()
			{
				return this.clientSite;
			}

			[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
			internal int GetControlInfo(NativeMethods.tagCONTROLINFO pCI)
			{
				if (this.accelCount == -1)
				{
					ArrayList arrayList = new ArrayList();
					this.GetMnemonicList(this.control, arrayList);
					this.accelCount = (short)arrayList.Count;
					if (this.accelCount > 0)
					{
						int num = UnsafeNativeMethods.SizeOf(typeof(NativeMethods.ACCEL));
						IntPtr intPtr = Marshal.AllocHGlobal(num * (int)this.accelCount * 2);
						try
						{
							NativeMethods.ACCEL aCCEL = new NativeMethods.ACCEL();
							aCCEL.cmd = 0;
							this.accelCount = 0;
							foreach (char c in arrayList)
							{
								IntPtr intPtr2 = (IntPtr)((long)intPtr + (long)((int)this.accelCount * num));
								if (c >= 'A' && c <= 'Z')
								{
									aCCEL.fVirt = 17;
									aCCEL.key = (UnsafeNativeMethods.VkKeyScan(c) & 255);
									Marshal.StructureToPtr(aCCEL, intPtr2, false);
									this.accelCount += 1;
									intPtr2 = (IntPtr)((long)intPtr2 + (long)num);
									aCCEL.fVirt = 21;
									Marshal.StructureToPtr(aCCEL, intPtr2, false);
								}
								else
								{
									aCCEL.fVirt = 17;
									short num2 = UnsafeNativeMethods.VkKeyScan(c);
									if ((num2 & 256) != 0)
									{
										NativeMethods.ACCEL expr_11E = aCCEL;
										expr_11E.fVirt |= 4;
									}
									aCCEL.key = (num2 & 255);
									Marshal.StructureToPtr(aCCEL, intPtr2, false);
								}
								NativeMethods.ACCEL expr_145 = aCCEL;
								expr_145.cmd += 1;
								this.accelCount += 1;
							}
							if (this.accelTable != IntPtr.Zero)
							{
								UnsafeNativeMethods.DestroyAcceleratorTable(new HandleRef(this, this.accelTable));
								this.accelTable = IntPtr.Zero;
							}
							this.accelTable = UnsafeNativeMethods.CreateAcceleratorTable(new HandleRef(null, intPtr), (int)this.accelCount);
						}
						finally
						{
							if (intPtr != IntPtr.Zero)
							{
								Marshal.FreeHGlobal(intPtr);
							}
						}
					}
				}
				pCI.cAccel = this.accelCount;
				pCI.hAccel = this.accelTable;
				return 0;
			}

			internal void GetExtent(int dwDrawAspect, NativeMethods.tagSIZEL pSizel)
			{
				if ((dwDrawAspect & 1) != 0)
				{
					Size size = this.control.Size;
					Point point = this.PixelToHiMetric(size.Width, size.Height);
					pSizel.cx = point.X;
					pSizel.cy = point.Y;
					return;
				}
				Control.ActiveXImpl.ThrowHr(-2147221397);
			}

			private void GetMnemonicList(Control control, ArrayList mnemonicList)
			{
				char mnemonic = WindowsFormsUtils.GetMnemonic(control.Text, true);
				if (mnemonic != '\0')
				{
					mnemonicList.Add(mnemonic);
				}
				foreach (Control control2 in control.Controls)
				{
					if (control2 != null)
					{
						this.GetMnemonicList(control2, mnemonicList);
					}
				}
			}

			private string GetStreamName()
			{
				string text = this.control.GetType().FullName;
				int length = text.Length;
				if (length > 31)
				{
					text = text.Substring(length - 31);
				}
				return text;
			}

			internal int GetWindow(out IntPtr hwnd)
			{
				if (!this.activeXState[Control.ActiveXImpl.inPlaceActive])
				{
					hwnd = IntPtr.Zero;
					return -2147467259;
				}
				hwnd = this.control.Handle;
				return 0;
			}

			private Point HiMetricToPixel(int x, int y)
			{
				return new Point
				{
					X = (this.LogPixels.X * x + Control.ActiveXImpl.hiMetricPerInch / 2) / Control.ActiveXImpl.hiMetricPerInch,
					Y = (this.LogPixels.Y * y + Control.ActiveXImpl.hiMetricPerInch / 2) / Control.ActiveXImpl.hiMetricPerInch
				};
			}

			[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
			internal void InPlaceActivate(int verb)
			{
				UnsafeNativeMethods.IOleInPlaceSite oleInPlaceSite = this.clientSite as UnsafeNativeMethods.IOleInPlaceSite;
				if (oleInPlaceSite == null)
				{
					return;
				}
				if (!this.activeXState[Control.ActiveXImpl.inPlaceActive])
				{
					int num = oleInPlaceSite.CanInPlaceActivate();
					if (num != 0)
					{
						if (NativeMethods.Succeeded(num))
						{
							num = -2147467259;
						}
						Control.ActiveXImpl.ThrowHr(num);
					}
					oleInPlaceSite.OnInPlaceActivate();
					this.activeXState[Control.ActiveXImpl.inPlaceActive] = true;
				}
				if (!this.activeXState[Control.ActiveXImpl.inPlaceVisible])
				{
					NativeMethods.tagOIFI tagOIFI = new NativeMethods.tagOIFI();
					tagOIFI.cb = UnsafeNativeMethods.SizeOf(typeof(NativeMethods.tagOIFI));
					IntPtr handle = IntPtr.Zero;
					handle = oleInPlaceSite.GetWindow();
					NativeMethods.COMRECT lprcPosRect = new NativeMethods.COMRECT();
					NativeMethods.COMRECT lprcClipRect = new NativeMethods.COMRECT();
					if (this.inPlaceUiWindow != null && UnsafeNativeMethods.IsComObject(this.inPlaceUiWindow))
					{
						UnsafeNativeMethods.ReleaseComObject(this.inPlaceUiWindow);
						this.inPlaceUiWindow = null;
					}
					if (this.inPlaceFrame != null && UnsafeNativeMethods.IsComObject(this.inPlaceFrame))
					{
						UnsafeNativeMethods.ReleaseComObject(this.inPlaceFrame);
						this.inPlaceFrame = null;
					}
					UnsafeNativeMethods.IOleInPlaceFrame oleInPlaceFrame;
					UnsafeNativeMethods.IOleInPlaceUIWindow oleInPlaceUIWindow;
					oleInPlaceSite.GetWindowContext(out oleInPlaceFrame, out oleInPlaceUIWindow, lprcPosRect, lprcClipRect, tagOIFI);
					this.SetObjectRects(lprcPosRect, lprcClipRect);
					this.inPlaceFrame = oleInPlaceFrame;
					this.inPlaceUiWindow = oleInPlaceUIWindow;
					this.hwndParent = handle;
					UnsafeNativeMethods.SetParent(new HandleRef(this.control, this.control.Handle), new HandleRef(null, handle));
					this.control.CreateControl();
					this.clientSite.ShowObject();
					this.SetInPlaceVisible(true);
				}
				if (verb != 0 && verb != -4)
				{
					return;
				}
				if (!this.activeXState[Control.ActiveXImpl.uiActive])
				{
					this.activeXState[Control.ActiveXImpl.uiActive] = true;
					oleInPlaceSite.OnUIActivate();
					if (!this.control.ContainsFocus)
					{
						this.control.FocusInternal();
					}
					this.inPlaceFrame.SetActiveObject(this.control, null);
					if (this.inPlaceUiWindow != null)
					{
						this.inPlaceUiWindow.SetActiveObject(this.control, null);
					}
					int num2 = this.inPlaceFrame.SetBorderSpace(null);
					if (NativeMethods.Failed(num2) && num2 != -2147221491 && num2 != -2147221087 && num2 != -2147467263)
					{
						UnsafeNativeMethods.ThrowExceptionForHR(num2);
					}
					if (this.inPlaceUiWindow != null)
					{
						num2 = this.inPlaceFrame.SetBorderSpace(null);
						if (NativeMethods.Failed(num2) && num2 != -2147221491 && num2 != -2147221087 && num2 != -2147467263)
						{
							UnsafeNativeMethods.ThrowExceptionForHR(num2);
						}
					}
				}
			}

			internal void InPlaceDeactivate()
			{
				if (!this.activeXState[Control.ActiveXImpl.inPlaceActive])
				{
					return;
				}
				if (this.activeXState[Control.ActiveXImpl.uiActive])
				{
					this.UIDeactivate();
				}
				this.activeXState[Control.ActiveXImpl.inPlaceActive] = false;
				this.activeXState[Control.ActiveXImpl.inPlaceVisible] = false;
				UnsafeNativeMethods.IOleInPlaceSite oleInPlaceSite = this.clientSite as UnsafeNativeMethods.IOleInPlaceSite;
				if (oleInPlaceSite != null)
				{
					IntSecurity.UnmanagedCode.Assert();
					try
					{
						oleInPlaceSite.OnInPlaceDeactivate();
					}
					finally
					{
						CodeAccessPermission.RevertAssert();
					}
				}
				this.control.Visible = false;
				this.hwndParent = IntPtr.Zero;
				if (this.inPlaceUiWindow != null && UnsafeNativeMethods.IsComObject(this.inPlaceUiWindow))
				{
					UnsafeNativeMethods.ReleaseComObject(this.inPlaceUiWindow);
					this.inPlaceUiWindow = null;
				}
				if (this.inPlaceFrame != null && UnsafeNativeMethods.IsComObject(this.inPlaceFrame))
				{
					UnsafeNativeMethods.ReleaseComObject(this.inPlaceFrame);
					this.inPlaceFrame = null;
				}
			}

			internal int IsDirty()
			{
				if (this.activeXState[Control.ActiveXImpl.isDirty])
				{
					return 0;
				}
				return 1;
			}

			private bool IsResourceProp(PropertyDescriptor prop)
			{
				TypeConverter converter = prop.Converter;
				Type[] array = new Type[]
				{
					typeof(string),
					typeof(byte[])
				};
				for (int i = 0; i < array.Length; i++)
				{
					Type type = array[i];
					if (converter.CanConvertTo(type) && converter.CanConvertFrom(type))
					{
						return false;
					}
				}
				return prop.GetValue(this.control) is ISerializable;
			}

			internal void Load(UnsafeNativeMethods.IStorage stg)
			{
				UnsafeNativeMethods.IStream stream = null;
				IntSecurity.UnmanagedCode.Assert();
				try
				{
					stream = stg.OpenStream(this.GetStreamName(), IntPtr.Zero, 16, 0);
				}
				catch (COMException arg_23_0)
				{
					if (arg_23_0.ErrorCode != -2147287038)
					{
						throw;
					}
					stream = stg.OpenStream(base.GetType().FullName, IntPtr.Zero, 16, 0);
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
				}
				this.Load(stream);
				stream = null;
				if (UnsafeNativeMethods.IsComObject(stg))
				{
					UnsafeNativeMethods.ReleaseComObject(stg);
				}
			}

			internal void Load(UnsafeNativeMethods.IStream stream)
			{
				Control.ActiveXImpl.PropertyBagStream propertyBagStream = new Control.ActiveXImpl.PropertyBagStream();
				propertyBagStream.Read(stream);
				this.Load(propertyBagStream, null);
				if (UnsafeNativeMethods.IsComObject(stream))
				{
					UnsafeNativeMethods.ReleaseComObject(stream);
				}
			}

			internal void Load(UnsafeNativeMethods.IPropertyBag pPropBag, UnsafeNativeMethods.IErrorLog pErrorLog)
			{
				PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this.control, new Attribute[]
				{
					DesignerSerializationVisibilityAttribute.Visible
				});
				for (int i = 0; i < properties.Count; i++)
				{
					try
					{
						object obj = null;
						int hr = -2147467259;
						IntSecurity.UnmanagedCode.Assert();
						try
						{
							hr = pPropBag.Read(properties[i].Name, ref obj, pErrorLog);
						}
						finally
						{
							CodeAccessPermission.RevertAssert();
						}
						if (NativeMethods.Succeeded(hr) && obj != null)
						{
							string text = null;
							int scode = 0;
							try
							{
								if (obj.GetType() != typeof(string))
								{
									obj = Convert.ToString(obj, CultureInfo.InvariantCulture);
								}
								if (this.IsResourceProp(properties[i]))
								{
									MemoryStream serializationStream = new MemoryStream(Convert.FromBase64String(obj.ToString()));
									BinaryFormatter binaryFormatter = new BinaryFormatter();
									properties[i].SetValue(this.control, binaryFormatter.Deserialize(serializationStream));
								}
								else
								{
									TypeConverter converter = properties[i].Converter;
									object value = null;
									if (converter.CanConvertFrom(typeof(string)))
									{
										value = converter.ConvertFromInvariantString(obj.ToString());
									}
									else if (converter.CanConvertFrom(typeof(byte[])))
									{
										string text2 = obj.ToString();
										value = converter.ConvertFrom(null, CultureInfo.InvariantCulture, Control.ActiveXImpl.FromBase64WrappedString(text2));
									}
									properties[i].SetValue(this.control, value);
								}
							}
							catch (Exception ex)
							{
								text = ex.ToString();
								if (ex is ExternalException)
								{
									scode = ((ExternalException)ex).ErrorCode;
								}
								else
								{
									scode = -2147467259;
								}
							}
							if (text != null && pErrorLog != null)
							{
								NativeMethods.tagEXCEPINFO tagEXCEPINFO = new NativeMethods.tagEXCEPINFO();
								tagEXCEPINFO.bstrSource = this.control.GetType().FullName;
								tagEXCEPINFO.bstrDescription = text;
								tagEXCEPINFO.scode = scode;
								pErrorLog.AddError(properties[i].Name, tagEXCEPINFO);
							}
						}
					}
					catch (Exception arg_1C7_0)
					{
						if (ClientUtils.IsSecurityOrCriticalException(arg_1C7_0))
						{
							throw;
						}
					}
				}
				if (UnsafeNativeMethods.IsComObject(pPropBag))
				{
					UnsafeNativeMethods.ReleaseComObject(pPropBag);
				}
			}

			private Control.AmbientProperty LookupAmbient(int dispid)
			{
				for (int i = 0; i < this.ambientProperties.Length; i++)
				{
					if (this.ambientProperties[i].DispID == dispid)
					{
						return this.ambientProperties[i];
					}
				}
				return this.ambientProperties[0];
			}

			internal IntPtr MergeRegion(IntPtr region)
			{
				if (this.clipRegion == IntPtr.Zero)
				{
					return region;
				}
				if (region == IntPtr.Zero)
				{
					return this.clipRegion;
				}
				IntPtr result;
				try
				{
					IntPtr intPtr = SafeNativeMethods.CreateRectRgn(0, 0, 0, 0);
					try
					{
						SafeNativeMethods.CombineRgn(new HandleRef(null, intPtr), new HandleRef(null, region), new HandleRef(this, this.clipRegion), 4);
						SafeNativeMethods.DeleteObject(new HandleRef(null, region));
					}
					catch
					{
						SafeNativeMethods.DeleteObject(new HandleRef(null, intPtr));
						throw;
					}
					result = intPtr;
				}
				catch (Exception arg_77_0)
				{
					if (ClientUtils.IsSecurityOrCriticalException(arg_77_0))
					{
						throw;
					}
					result = region;
				}
				return result;
			}

			private void CallParentPropertyChanged(Control control, string propName)
			{
				uint num = <PrivateImplementationDetails>.ComputeStringHash(propName);
				if (num <= 2626085950u)
				{
					if (num <= 777198197u)
					{
						if (num != 41545325u)
						{
							if (num != 777198197u)
							{
								return;
							}
							if (!(propName == "BackColor"))
							{
								return;
							}
							control.OnParentBackColorChanged(EventArgs.Empty);
							return;
						}
						else
						{
							if (!(propName == "BindingContext"))
							{
								return;
							}
							control.OnParentBindingContextChanged(EventArgs.Empty);
							return;
						}
					}
					else if (num != 1495943489u)
					{
						if (num != 2626085950u)
						{
							return;
						}
						if (!(propName == "Enabled"))
						{
							return;
						}
						control.OnParentEnabledChanged(EventArgs.Empty);
						return;
					}
					else
					{
						if (!(propName == "Visible"))
						{
							return;
						}
						control.OnParentVisibleChanged(EventArgs.Empty);
						return;
					}
				}
				else if (num <= 2936102910u)
				{
					if (num != 2809814704u)
					{
						if (num != 2936102910u)
						{
							return;
						}
						if (!(propName == "ForeColor"))
						{
							return;
						}
						control.OnParentForeColorChanged(EventArgs.Empty);
						return;
					}
					else
					{
						if (!(propName == "Font"))
						{
							return;
						}
						control.OnParentFontChanged(EventArgs.Empty);
						return;
					}
				}
				else if (num != 3049818181u)
				{
					if (num != 3770400898u)
					{
						return;
					}
					if (!(propName == "BackgroundImage"))
					{
						return;
					}
					control.OnParentBackgroundImageChanged(EventArgs.Empty);
					return;
				}
				else
				{
					if (!(propName == "RightToLeft"))
					{
						return;
					}
					control.OnParentRightToLeftChanged(EventArgs.Empty);
					return;
				}
			}

			internal void OnAmbientPropertyChange(int dispID)
			{
				if (dispID != -1)
				{
					for (int i = 0; i < this.ambientProperties.Length; i++)
					{
						if (this.ambientProperties[i].DispID == dispID)
						{
							this.ambientProperties[i].ResetValue();
							this.CallParentPropertyChanged(this.control, this.ambientProperties[i].Name);
							return;
						}
					}
					object obj = new object();
					if (dispID != -713)
					{
						if (dispID == -710 && this.GetAmbientProperty(-710, ref obj))
						{
							this.activeXState[Control.ActiveXImpl.uiDead] = (bool)obj;
							return;
						}
					}
					else
					{
						IButtonControl buttonControl = this.control as IButtonControl;
						if (buttonControl != null && this.GetAmbientProperty(-713, ref obj))
						{
							buttonControl.NotifyDefault((bool)obj);
							return;
						}
					}
				}
				else
				{
					for (int j = 0; j < this.ambientProperties.Length; j++)
					{
						this.ambientProperties[j].ResetValue();
						this.CallParentPropertyChanged(this.control, this.ambientProperties[j].Name);
					}
				}
			}

			internal void OnDocWindowActivate(int fActivate)
			{
				if (this.activeXState[Control.ActiveXImpl.uiActive] && fActivate != 0 && this.inPlaceFrame != null)
				{
					IntSecurity.UnmanagedCode.Assert();
					int num;
					try
					{
						num = this.inPlaceFrame.SetBorderSpace(null);
					}
					finally
					{
						CodeAccessPermission.RevertAssert();
					}
					if (NativeMethods.Failed(num) && num != -2147221087 && num != -2147467263)
					{
						UnsafeNativeMethods.ThrowExceptionForHR(num);
					}
				}
			}

			internal void OnFocus(bool focus)
			{
				if (this.activeXState[Control.ActiveXImpl.inPlaceActive] && this.clientSite is UnsafeNativeMethods.IOleControlSite)
				{
					IntSecurity.UnmanagedCode.Assert();
					try
					{
						((UnsafeNativeMethods.IOleControlSite)this.clientSite).OnFocus(focus ? 1 : 0);
					}
					finally
					{
						CodeAccessPermission.RevertAssert();
					}
				}
				if (focus && this.activeXState[Control.ActiveXImpl.inPlaceActive] && !this.activeXState[Control.ActiveXImpl.uiActive])
				{
					this.InPlaceActivate(-4);
				}
			}

			private Point PixelToHiMetric(int x, int y)
			{
				return new Point
				{
					X = (Control.ActiveXImpl.hiMetricPerInch * x + (this.LogPixels.X >> 1)) / this.LogPixels.X,
					Y = (Control.ActiveXImpl.hiMetricPerInch * y + (this.LogPixels.Y >> 1)) / this.LogPixels.Y
				};
			}

			internal void QuickActivate(UnsafeNativeMethods.tagQACONTAINER pQaContainer, UnsafeNativeMethods.tagQACONTROL pQaControl)
			{
				Control.AmbientProperty ambientProperty = this.LookupAmbient(-701);
				ambientProperty.Value = ColorTranslator.FromOle((int)pQaContainer.colorBack);
				ambientProperty = this.LookupAmbient(-704);
				ambientProperty.Value = ColorTranslator.FromOle((int)pQaContainer.colorFore);
				if (pQaContainer.pFont != null)
				{
					ambientProperty = this.LookupAmbient(-703);
					IntSecurity.UnmanagedCode.Assert();
					try
					{
						IntPtr arg_67_0 = IntPtr.Zero;
						Font value = Font.FromHfont(((UnsafeNativeMethods.IFont)pQaContainer.pFont).GetHFont());
						ambientProperty.Value = value;
					}
					catch (Exception arg_87_0)
					{
						if (ClientUtils.IsSecurityOrCriticalException(arg_87_0))
						{
							throw;
						}
						ambientProperty.Value = null;
					}
					finally
					{
						CodeAccessPermission.RevertAssert();
					}
				}
				pQaControl.cbSize = UnsafeNativeMethods.SizeOf(typeof(UnsafeNativeMethods.tagQACONTROL));
				this.SetClientSite(pQaContainer.pClientSite);
				if (pQaContainer.pAdviseSink != null)
				{
					this.SetAdvise(1, 0, (IAdviseSink)pQaContainer.pAdviseSink);
				}
				IntSecurity.UnmanagedCode.Assert();
				int dwMiscStatus;
				try
				{
					((UnsafeNativeMethods.IOleObject)this.control).GetMiscStatus(1, out dwMiscStatus);
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
				}
				pQaControl.dwMiscStatus = dwMiscStatus;
				if (pQaContainer.pUnkEventSink != null && this.control is UserControl)
				{
					Type defaultEventsInterface = Control.ActiveXImpl.GetDefaultEventsInterface(this.control.GetType());
					if (defaultEventsInterface != null)
					{
						IntSecurity.UnmanagedCode.Assert();
						try
						{
							Control.ActiveXImpl.AdviseHelper.AdviseConnectionPoint(this.control, pQaContainer.pUnkEventSink, defaultEventsInterface, out pQaControl.dwEventCookie);
						}
						catch (Exception arg_157_0)
						{
							if (ClientUtils.IsSecurityOrCriticalException(arg_157_0))
							{
								throw;
							}
						}
						finally
						{
							CodeAccessPermission.RevertAssert();
						}
					}
				}
				if (pQaContainer.pPropertyNotifySink != null && UnsafeNativeMethods.IsComObject(pQaContainer.pPropertyNotifySink))
				{
					UnsafeNativeMethods.ReleaseComObject(pQaContainer.pPropertyNotifySink);
				}
				if (pQaContainer.pUnkEventSink != null && UnsafeNativeMethods.IsComObject(pQaContainer.pUnkEventSink))
				{
					UnsafeNativeMethods.ReleaseComObject(pQaContainer.pUnkEventSink);
				}
			}

			private static Type GetDefaultEventsInterface(Type controlType)
			{
				Type type = null;
				object[] customAttributes = controlType.GetCustomAttributes(typeof(ComSourceInterfacesAttribute), false);
				if (customAttributes.Length != 0)
				{
					string text = ((ComSourceInterfacesAttribute)customAttributes[0]).Value.Split(new char[1])[0];
					type = controlType.Module.Assembly.GetType(text, false);
					if (type == null)
					{
						type = Type.GetType(text, false);
					}
				}
				return type;
			}

			internal void Save(UnsafeNativeMethods.IStorage stg, bool fSameAsLoad)
			{
				UnsafeNativeMethods.IStream stream = null;
				IntSecurity.UnmanagedCode.Assert();
				try
				{
					stream = stg.CreateStream(this.GetStreamName(), 4113, 0, 0);
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
				}
				this.Save(stream, true);
				UnsafeNativeMethods.ReleaseComObject(stream);
			}

			internal void Save(UnsafeNativeMethods.IStream stream, bool fClearDirty)
			{
				Control.ActiveXImpl.PropertyBagStream propertyBagStream = new Control.ActiveXImpl.PropertyBagStream();
				this.Save(propertyBagStream, fClearDirty, false);
				IntSecurity.UnmanagedCode.Assert();
				try
				{
					propertyBagStream.Write(stream);
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
				}
				if (UnsafeNativeMethods.IsComObject(stream))
				{
					UnsafeNativeMethods.ReleaseComObject(stream);
				}
			}

			[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
			internal void Save(UnsafeNativeMethods.IPropertyBag pPropBag, bool fClearDirty, bool fSaveAllProperties)
			{
				PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this.control, new Attribute[]
				{
					DesignerSerializationVisibilityAttribute.Visible
				});
				for (int i = 0; i < properties.Count; i++)
				{
					if (fSaveAllProperties || properties[i].ShouldSerializeValue(this.control))
					{
						if (this.IsResourceProp(properties[i]))
						{
							MemoryStream memoryStream = new MemoryStream();
							new BinaryFormatter().Serialize(memoryStream, properties[i].GetValue(this.control));
							byte[] array = new byte[(int)memoryStream.Length];
							memoryStream.Position = 0L;
							memoryStream.Read(array, 0, array.Length);
							object obj = Convert.ToBase64String(array);
							pPropBag.Write(properties[i].Name, ref obj);
						}
						else
						{
							TypeConverter converter = properties[i].Converter;
							if (converter.CanConvertFrom(typeof(string)))
							{
								object obj = converter.ConvertToInvariantString(properties[i].GetValue(this.control));
								pPropBag.Write(properties[i].Name, ref obj);
							}
							else if (converter.CanConvertFrom(typeof(byte[])))
							{
								object obj = Convert.ToBase64String((byte[])converter.ConvertTo(null, CultureInfo.InvariantCulture, properties[i].GetValue(this.control), typeof(byte[])));
								pPropBag.Write(properties[i].Name, ref obj);
							}
						}
					}
				}
				if (UnsafeNativeMethods.IsComObject(pPropBag))
				{
					UnsafeNativeMethods.ReleaseComObject(pPropBag);
				}
				if (fClearDirty)
				{
					this.activeXState[Control.ActiveXImpl.isDirty] = false;
				}
			}

			private void SendOnSave()
			{
				int count = this.adviseList.Count;
				IntSecurity.UnmanagedCode.Assert();
				for (int i = 0; i < count; i++)
				{
					((IAdviseSink)this.adviseList[i]).OnSave();
				}
			}

			internal void SetAdvise(int aspects, int advf, IAdviseSink pAdvSink)
			{
				if ((aspects & 1) == 0)
				{
					Control.ActiveXImpl.ThrowHr(-2147221397);
				}
				this.activeXState[Control.ActiveXImpl.viewAdvisePrimeFirst] = ((advf & 2) != 0);
				this.activeXState[Control.ActiveXImpl.viewAdviseOnlyOnce] = ((advf & 4) != 0);
				if (this.viewAdviseSink != null && UnsafeNativeMethods.IsComObject(this.viewAdviseSink))
				{
					UnsafeNativeMethods.ReleaseComObject(this.viewAdviseSink);
				}
				this.viewAdviseSink = pAdvSink;
				if (this.activeXState[Control.ActiveXImpl.viewAdvisePrimeFirst])
				{
					this.ViewChanged();
				}
			}

			internal void SetClientSite(UnsafeNativeMethods.IOleClientSite value)
			{
				if (this.clientSite != null)
				{
					if (value == null)
					{
						Control.ActiveXImpl.globalActiveXCount--;
						if (Control.ActiveXImpl.globalActiveXCount == 0 && this.IsIE)
						{
							new PermissionSet(PermissionState.Unrestricted).Assert();
							try
							{
								MethodInfo method = typeof(SystemEvents).GetMethod("Shutdown", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, new Type[0], new ParameterModifier[0]);
								if (method != null)
								{
									method.Invoke(null, null);
								}
							}
							finally
							{
								CodeAccessPermission.RevertAssert();
							}
						}
					}
					if (UnsafeNativeMethods.IsComObject(this.clientSite))
					{
						IntSecurity.UnmanagedCode.Assert();
						try
						{
							Marshal.FinalReleaseComObject(this.clientSite);
						}
						finally
						{
							CodeAccessPermission.RevertAssert();
						}
					}
				}
				this.clientSite = value;
				if (this.clientSite != null)
				{
					this.control.Site = new Control.AxSourcingSite(this.control, this.clientSite, "ControlAxSourcingSite");
				}
				else
				{
					this.control.Site = null;
				}
				object obj = new object();
				if (this.GetAmbientProperty(-710, ref obj))
				{
					this.activeXState[Control.ActiveXImpl.uiDead] = (bool)obj;
				}
				if (this.control is IButtonControl && this.GetAmbientProperty(-710, ref obj))
				{
					((IButtonControl)this.control).NotifyDefault((bool)obj);
				}
				if (this.clientSite == null)
				{
					if (this.accelTable != IntPtr.Zero)
					{
						UnsafeNativeMethods.DestroyAcceleratorTable(new HandleRef(this, this.accelTable));
						this.accelTable = IntPtr.Zero;
						this.accelCount = -1;
					}
					if (this.IsIE)
					{
						this.control.Dispose();
					}
				}
				else
				{
					Control.ActiveXImpl.globalActiveXCount++;
					if (Control.ActiveXImpl.globalActiveXCount == 1 && this.IsIE)
					{
						new PermissionSet(PermissionState.Unrestricted).Assert();
						try
						{
							MethodInfo method2 = typeof(SystemEvents).GetMethod("Startup", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, new Type[0], new ParameterModifier[0]);
							if (method2 != null)
							{
								method2.Invoke(null, null);
							}
						}
						finally
						{
							CodeAccessPermission.RevertAssert();
						}
					}
				}
				this.control.OnTopMostActiveXParentChanged(EventArgs.Empty);
			}

			[SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
			internal void SetExtent(int dwDrawAspect, NativeMethods.tagSIZEL pSizel)
			{
				if ((dwDrawAspect & 1) != 0)
				{
					if (this.activeXState[Control.ActiveXImpl.changingExtents])
					{
						return;
					}
					this.activeXState[Control.ActiveXImpl.changingExtents] = true;
					try
					{
						Size size = new Size(this.HiMetricToPixel(pSizel.cx, pSizel.cy));
						if (this.activeXState[Control.ActiveXImpl.inPlaceActive])
						{
							UnsafeNativeMethods.IOleInPlaceSite oleInPlaceSite = this.clientSite as UnsafeNativeMethods.IOleInPlaceSite;
							if (oleInPlaceSite != null)
							{
								Rectangle bounds = this.control.Bounds;
								bounds.Location = new Point(bounds.X, bounds.Y);
								Size size2 = new Size(size.Width, size.Height);
								bounds.Width = size2.Width;
								bounds.Height = size2.Height;
								oleInPlaceSite.OnPosRectChange(NativeMethods.COMRECT.FromXYWH(bounds.X, bounds.Y, bounds.Width, bounds.Height));
							}
						}
						this.control.Size = size;
						if (!this.control.Size.Equals(size))
						{
							this.activeXState[Control.ActiveXImpl.isDirty] = true;
							if (!this.activeXState[Control.ActiveXImpl.inPlaceActive])
							{
								this.ViewChanged();
							}
							if (!this.activeXState[Control.ActiveXImpl.inPlaceActive] && this.clientSite != null)
							{
								this.clientSite.RequestNewObjectLayout();
							}
						}
						return;
					}
					finally
					{
						this.activeXState[Control.ActiveXImpl.changingExtents] = false;
					}
				}
				Control.ActiveXImpl.ThrowHr(-2147221397);
			}

			private void SetInPlaceVisible(bool visible)
			{
				this.activeXState[Control.ActiveXImpl.inPlaceVisible] = visible;
				this.control.Visible = visible;
			}

			internal void SetObjectRects(NativeMethods.COMRECT lprcPosRect, NativeMethods.COMRECT lprcClipRect)
			{
				Rectangle rectangle = Rectangle.FromLTRB(lprcPosRect.left, lprcPosRect.top, lprcPosRect.right, lprcPosRect.bottom);
				if (this.activeXState[Control.ActiveXImpl.adjustingRect])
				{
					this.adjustRect.left = rectangle.X;
					this.adjustRect.top = rectangle.Y;
					this.adjustRect.right = rectangle.Width + rectangle.X;
					this.adjustRect.bottom = rectangle.Height + rectangle.Y;
				}
				else
				{
					this.activeXState[Control.ActiveXImpl.adjustingRect] = true;
					try
					{
						this.control.Bounds = rectangle;
					}
					finally
					{
						this.activeXState[Control.ActiveXImpl.adjustingRect] = false;
					}
				}
				bool flag = false;
				if (this.clipRegion != IntPtr.Zero)
				{
					this.clipRegion = IntPtr.Zero;
					flag = true;
				}
				if (lprcClipRect != null)
				{
					Rectangle b = Rectangle.FromLTRB(lprcClipRect.left, lprcClipRect.top, lprcClipRect.right, lprcClipRect.bottom);
					Rectangle rectangle2;
					if (!b.IsEmpty)
					{
						rectangle2 = Rectangle.Intersect(rectangle, b);
					}
					else
					{
						rectangle2 = rectangle;
					}
					if (!rectangle2.Equals(rectangle))
					{
						NativeMethods.RECT rECT = NativeMethods.RECT.FromXYWH(rectangle2.X, rectangle2.Y, rectangle2.Width, rectangle2.Height);
						IntPtr parent = UnsafeNativeMethods.GetParent(new HandleRef(this.control, this.control.Handle));
						UnsafeNativeMethods.MapWindowPoints(new HandleRef(null, parent), new HandleRef(this.control, this.control.Handle), ref rECT, 2);
						this.clipRegion = SafeNativeMethods.CreateRectRgn(rECT.left, rECT.top, rECT.right, rECT.bottom);
						flag = true;
					}
				}
				if (flag && this.control.IsHandleCreated)
				{
					IntPtr handle = this.clipRegion;
					Region region = this.control.Region;
					if (region != null)
					{
						IntPtr hRgn = this.control.GetHRgn(region);
						handle = this.MergeRegion(hRgn);
					}
					UnsafeNativeMethods.SetWindowRgn(new HandleRef(this.control, this.control.Handle), new HandleRef(this, handle), SafeNativeMethods.IsWindowVisible(new HandleRef(this.control, this.control.Handle)));
				}
				this.control.Invalidate();
			}

			internal static void ThrowHr(int hr)
			{
				throw new ExternalException(SR.GetString("ExternalException"), hr);
			}

			internal int TranslateAccelerator(ref NativeMethods.MSG lpmsg)
			{
				bool flag = false;
				switch (lpmsg.message)
				{
				case 256:
				case 258:
				case 260:
				case 262:
					flag = true;
					break;
				}
				Message message = Message.Create(lpmsg.hwnd, lpmsg.message, lpmsg.wParam, lpmsg.lParam);
				if (flag)
				{
					Control control = Control.FromChildHandleInternal(lpmsg.hwnd);
					if (control != null && (this.control == control || this.control.Contains(control)))
					{
						switch (Control.PreProcessControlMessageInternal(control, ref message))
						{
						case PreProcessControlState.MessageProcessed:
							lpmsg.message = message.Msg;
							lpmsg.wParam = message.WParam;
							lpmsg.lParam = message.LParam;
							return 0;
						case PreProcessControlState.MessageNeeded:
							UnsafeNativeMethods.TranslateMessage(ref lpmsg);
							if (SafeNativeMethods.IsWindowUnicode(new HandleRef(null, lpmsg.hwnd)))
							{
								UnsafeNativeMethods.DispatchMessageW(ref lpmsg);
							}
							else
							{
								UnsafeNativeMethods.DispatchMessageA(ref lpmsg);
							}
							return 0;
						}
					}
				}
				int result = 1;
				UnsafeNativeMethods.IOleControlSite oleControlSite = this.clientSite as UnsafeNativeMethods.IOleControlSite;
				if (oleControlSite != null)
				{
					int num = 0;
					if (UnsafeNativeMethods.GetKeyState(16) < 0)
					{
						num |= 1;
					}
					if (UnsafeNativeMethods.GetKeyState(17) < 0)
					{
						num |= 2;
					}
					if (UnsafeNativeMethods.GetKeyState(18) < 0)
					{
						num |= 4;
					}
					IntSecurity.UnmanagedCode.Assert();
					try
					{
						result = oleControlSite.TranslateAccelerator(ref lpmsg, num);
					}
					finally
					{
						CodeAccessPermission.RevertAssert();
					}
				}
				return result;
			}

			internal int UIDeactivate()
			{
				if (!this.activeXState[Control.ActiveXImpl.uiActive])
				{
					return 0;
				}
				this.activeXState[Control.ActiveXImpl.uiActive] = false;
				if (this.inPlaceUiWindow != null)
				{
					this.inPlaceUiWindow.SetActiveObject(null, null);
				}
				IntSecurity.UnmanagedCode.Assert();
				this.inPlaceFrame.SetActiveObject(null, null);
				UnsafeNativeMethods.IOleInPlaceSite oleInPlaceSite = this.clientSite as UnsafeNativeMethods.IOleInPlaceSite;
				if (oleInPlaceSite != null)
				{
					oleInPlaceSite.OnUIDeactivate(0);
				}
				return 0;
			}

			internal void Unadvise(int dwConnection)
			{
				if (dwConnection > this.adviseList.Count || this.adviseList[dwConnection - 1] == null)
				{
					Control.ActiveXImpl.ThrowHr(-2147221500);
				}
				IAdviseSink adviseSink = (IAdviseSink)this.adviseList[dwConnection - 1];
				this.adviseList.RemoveAt(dwConnection - 1);
				if (adviseSink != null && UnsafeNativeMethods.IsComObject(adviseSink))
				{
					UnsafeNativeMethods.ReleaseComObject(adviseSink);
				}
			}

			internal void UpdateBounds(ref int x, ref int y, ref int width, ref int height, int flags)
			{
				if (!this.activeXState[Control.ActiveXImpl.adjustingRect] && this.activeXState[Control.ActiveXImpl.inPlaceVisible])
				{
					UnsafeNativeMethods.IOleInPlaceSite oleInPlaceSite = this.clientSite as UnsafeNativeMethods.IOleInPlaceSite;
					if (oleInPlaceSite != null)
					{
						NativeMethods.COMRECT cOMRECT = new NativeMethods.COMRECT();
						if ((flags & 2) != 0)
						{
							cOMRECT.left = this.control.Left;
							cOMRECT.top = this.control.Top;
						}
						else
						{
							cOMRECT.left = x;
							cOMRECT.top = y;
						}
						if ((flags & 1) != 0)
						{
							cOMRECT.left += this.control.Width;
							cOMRECT.top += this.control.Height;
						}
						else
						{
							cOMRECT.left += width;
							cOMRECT.top += height;
						}
						this.adjustRect = cOMRECT;
						this.activeXState[Control.ActiveXImpl.adjustingRect] = true;
						IntSecurity.UnmanagedCode.Assert();
						try
						{
							oleInPlaceSite.OnPosRectChange(cOMRECT);
						}
						finally
						{
							CodeAccessPermission.RevertAssert();
							this.adjustRect = null;
							this.activeXState[Control.ActiveXImpl.adjustingRect] = false;
						}
						if ((flags & 2) == 0)
						{
							x = cOMRECT.left;
							y = cOMRECT.top;
						}
						if ((flags & 1) == 0)
						{
							width = cOMRECT.right - cOMRECT.left;
							height = cOMRECT.bottom - cOMRECT.top;
						}
					}
				}
			}

			internal void UpdateAccelTable()
			{
				this.accelCount = -1;
				UnsafeNativeMethods.IOleControlSite oleControlSite = this.clientSite as UnsafeNativeMethods.IOleControlSite;
				if (oleControlSite != null)
				{
					IntSecurity.UnmanagedCode.Assert();
					oleControlSite.OnControlInfoChanged();
				}
			}

			internal void ViewChangedInternal()
			{
				this.ViewChanged();
			}

			private void ViewChanged()
			{
				if (this.viewAdviseSink != null && !this.activeXState[Control.ActiveXImpl.saving])
				{
					IntSecurity.UnmanagedCode.Assert();
					try
					{
						this.viewAdviseSink.OnViewChange(1, -1);
					}
					finally
					{
						CodeAccessPermission.RevertAssert();
					}
					if (this.activeXState[Control.ActiveXImpl.viewAdviseOnlyOnce])
					{
						if (UnsafeNativeMethods.IsComObject(this.viewAdviseSink))
						{
							UnsafeNativeMethods.ReleaseComObject(this.viewAdviseSink);
						}
						this.viewAdviseSink = null;
					}
				}
			}

			void IWindowTarget.OnHandleChange(IntPtr newHandle)
			{
				this.controlWindowTarget.OnHandleChange(newHandle);
			}

			void IWindowTarget.OnMessage(ref Message m)
			{
				if (this.activeXState[Control.ActiveXImpl.uiDead])
				{
					if (m.Msg >= 512 && m.Msg <= 522)
					{
						return;
					}
					if (m.Msg >= 161 && m.Msg <= 169)
					{
						return;
					}
					if (m.Msg >= 256 && m.Msg <= 264)
					{
						return;
					}
				}
				IntSecurity.UnmanagedCode.Assert();
				this.controlWindowTarget.OnMessage(ref m);
			}
		}

		private class AxSourcingSite : ISite, IServiceProvider
		{
			private IComponent component;

			private UnsafeNativeMethods.IOleClientSite clientSite;

			private string name;

			private HtmlShimManager shimManager;

			public IComponent Component
			{
				get
				{
					return this.component;
				}
			}

			public IContainer Container
			{
				get
				{
					return null;
				}
			}

			public bool DesignMode
			{
				get
				{
					return false;
				}
			}

			public string Name
			{
				get
				{
					return this.name;
				}
				set
				{
					if (value == null || this.name == null)
					{
						this.name = value;
					}
				}
			}

			internal AxSourcingSite(IComponent component, UnsafeNativeMethods.IOleClientSite clientSite, string name)
			{
				this.component = component;
				this.clientSite = clientSite;
				this.name = name;
			}

			public object GetService(Type service)
			{
				object result = null;
				if (service == typeof(HtmlDocument))
				{
					UnsafeNativeMethods.IOleContainer oleContainer;
					int container;
					try
					{
						IntSecurity.UnmanagedCode.Assert();
						container = this.clientSite.GetContainer(out oleContainer);
					}
					finally
					{
						CodeAccessPermission.RevertAssert();
					}
					if (NativeMethods.Succeeded(container) && oleContainer is UnsafeNativeMethods.IHTMLDocument)
					{
						if (this.shimManager == null)
						{
							this.shimManager = new HtmlShimManager();
						}
						result = new HtmlDocument(this.shimManager, oleContainer as UnsafeNativeMethods.IHTMLDocument);
					}
				}
				else if (this.clientSite.GetType().IsAssignableFrom(service))
				{
					IntSecurity.UnmanagedCode.Demand();
					result = this.clientSite;
				}
				return result;
			}
		}

		private class ActiveXFontMarshaler : ICustomMarshaler
		{
			private static Control.ActiveXFontMarshaler instance;

			public void CleanUpManagedData(object obj)
			{
			}

			public void CleanUpNativeData(IntPtr pObj)
			{
				Marshal.Release(pObj);
			}

			internal static ICustomMarshaler GetInstance(string cookie)
			{
				if (Control.ActiveXFontMarshaler.instance == null)
				{
					Control.ActiveXFontMarshaler.instance = new Control.ActiveXFontMarshaler();
				}
				return Control.ActiveXFontMarshaler.instance;
			}

			public int GetNativeDataSize()
			{
				return -1;
			}

			public IntPtr MarshalManagedToNative(object obj)
			{
				Font font = (Font)obj;
				NativeMethods.tagFONTDESC tagFONTDESC = new NativeMethods.tagFONTDESC();
				NativeMethods.LOGFONT lOGFONT = new NativeMethods.LOGFONT();
				IntSecurity.ObjectFromWin32Handle.Assert();
				try
				{
					font.ToLogFont(lOGFONT);
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
				}
				tagFONTDESC.lpstrName = font.Name;
				tagFONTDESC.cySize = (long)(font.SizeInPoints * 10000f);
				tagFONTDESC.sWeight = (short)lOGFONT.lfWeight;
				tagFONTDESC.sCharset = (short)lOGFONT.lfCharSet;
				tagFONTDESC.fItalic = font.Italic;
				tagFONTDESC.fUnderline = font.Underline;
				tagFONTDESC.fStrikethrough = font.Strikeout;
				Guid gUID = typeof(UnsafeNativeMethods.IFont).GUID;
				IntPtr expr_A5 = Marshal.GetIUnknownForObject(UnsafeNativeMethods.OleCreateFontIndirect(tagFONTDESC, ref gUID));
				IntPtr result;
				int num = Marshal.QueryInterface(expr_A5, ref gUID, out result);
				Marshal.Release(expr_A5);
				if (NativeMethods.Failed(num))
				{
					Marshal.ThrowExceptionForHR(num);
				}
				return result;
			}

			public object MarshalNativeToManaged(IntPtr pObj)
			{
				UnsafeNativeMethods.IFont font = (UnsafeNativeMethods.IFont)Marshal.GetObjectForIUnknown(pObj);
				IntPtr hfont = IntPtr.Zero;
				IntSecurity.UnmanagedCode.Assert();
				try
				{
					hfont = font.GetHFont();
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
				}
				Font result = null;
				IntSecurity.ObjectFromWin32Handle.Assert();
				try
				{
					result = Font.FromHfont(hfont);
				}
				catch (Exception arg_40_0)
				{
					if (ClientUtils.IsSecurityOrCriticalException(arg_40_0))
					{
						throw;
					}
					result = Control.DefaultFont;
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
				}
				return result;
			}
		}

		private class ActiveXVerbEnum : UnsafeNativeMethods.IEnumOLEVERB
		{
			private NativeMethods.tagOLEVERB[] verbs;

			private int current;

			internal ActiveXVerbEnum(NativeMethods.tagOLEVERB[] verbs)
			{
				this.verbs = verbs;
				this.current = 0;
			}

			public int Next(int celt, NativeMethods.tagOLEVERB rgelt, int[] pceltFetched)
			{
				int num = 0;
				if (celt != 1)
				{
					celt = 1;
				}
				while (celt > 0 && this.current < this.verbs.Length)
				{
					rgelt.lVerb = this.verbs[this.current].lVerb;
					rgelt.lpszVerbName = this.verbs[this.current].lpszVerbName;
					rgelt.fuFlags = this.verbs[this.current].fuFlags;
					rgelt.grfAttribs = this.verbs[this.current].grfAttribs;
					celt--;
					this.current++;
					num++;
				}
				if (pceltFetched != null)
				{
					pceltFetched[0] = num;
				}
				if (celt != 0)
				{
					return 1;
				}
				return 0;
			}

			public int Skip(int celt)
			{
				if (this.current + celt < this.verbs.Length)
				{
					this.current += celt;
					return 0;
				}
				this.current = this.verbs.Length;
				return 1;
			}

			public void Reset()
			{
				this.current = 0;
			}

			public void Clone(out UnsafeNativeMethods.IEnumOLEVERB ppenum)
			{
				ppenum = new Control.ActiveXVerbEnum(this.verbs);
			}
		}

		private class AmbientProperty
		{
			private string name;

			private int dispID;

			private object value;

			private bool empty;

			internal string Name
			{
				get
				{
					return this.name;
				}
			}

			internal int DispID
			{
				get
				{
					return this.dispID;
				}
			}

			internal bool Empty
			{
				get
				{
					return this.empty;
				}
			}

			internal object Value
			{
				get
				{
					return this.value;
				}
				set
				{
					this.value = value;
					this.empty = false;
				}
			}

			internal AmbientProperty(string name, int dispID)
			{
				this.name = name;
				this.dispID = dispID;
				this.value = null;
				this.empty = true;
			}

			internal void ResetValue()
			{
				this.empty = true;
				this.value = null;
			}
		}

		private class MetafileDCWrapper : IDisposable
		{
			private HandleRef hBitmapDC = NativeMethods.NullHandleRef;

			private HandleRef hBitmap = NativeMethods.NullHandleRef;

			private HandleRef hOriginalBmp = NativeMethods.NullHandleRef;

			private HandleRef hMetafileDC = NativeMethods.NullHandleRef;

			private NativeMethods.RECT destRect;

			internal IntPtr HDC
			{
				get
				{
					return this.hBitmapDC.Handle;
				}
			}

			internal MetafileDCWrapper(HandleRef hOriginalDC, Size size)
			{
				if (size.Width < 0 || size.Height < 0)
				{
					throw new ArgumentException("size", SR.GetString("ControlMetaFileDCWrapperSizeInvalid"));
				}
				this.hMetafileDC = hOriginalDC;
				this.destRect = new NativeMethods.RECT(0, 0, size.Width, size.Height);
				this.hBitmapDC = new HandleRef(this, UnsafeNativeMethods.CreateCompatibleDC(NativeMethods.NullHandleRef));
				int deviceCaps = UnsafeNativeMethods.GetDeviceCaps(this.hBitmapDC, 14);
				int deviceCaps2 = UnsafeNativeMethods.GetDeviceCaps(this.hBitmapDC, 12);
				this.hBitmap = new HandleRef(this, SafeNativeMethods.CreateBitmap(size.Width, size.Height, deviceCaps, deviceCaps2, IntPtr.Zero));
				this.hOriginalBmp = new HandleRef(this, SafeNativeMethods.SelectObject(this.hBitmapDC, this.hBitmap));
			}

			~MetafileDCWrapper()
			{
				((IDisposable)this).Dispose();
			}

			void IDisposable.Dispose()
			{
				if (this.hBitmapDC.Handle == IntPtr.Zero || this.hMetafileDC.Handle == IntPtr.Zero || this.hBitmap.Handle == IntPtr.Zero)
				{
					return;
				}
				try
				{
					this.DICopy(this.hMetafileDC, this.hBitmapDC, this.destRect, true);
					SafeNativeMethods.SelectObject(this.hBitmapDC, this.hOriginalBmp);
					SafeNativeMethods.DeleteObject(this.hBitmap);
					UnsafeNativeMethods.DeleteCompatibleDC(this.hBitmapDC);
				}
				finally
				{
					this.hBitmapDC = NativeMethods.NullHandleRef;
					this.hBitmap = NativeMethods.NullHandleRef;
					this.hOriginalBmp = NativeMethods.NullHandleRef;
					GC.SuppressFinalize(this);
				}
			}

			private unsafe bool DICopy(HandleRef hdcDest, HandleRef hdcSrc, NativeMethods.RECT rect, bool bStretch)
			{
				bool flag = false;
				HandleRef hObject = new HandleRef(this, SafeNativeMethods.CreateBitmap(1, 1, 1, 1, IntPtr.Zero));
				if (hObject.Handle == IntPtr.Zero)
				{
					return flag;
				}
				try
				{
					HandleRef handleRef = new HandleRef(this, SafeNativeMethods.SelectObject(hdcSrc, hObject));
					if (handleRef.Handle == IntPtr.Zero)
					{
						bool result = flag;
						return result;
					}
					SafeNativeMethods.SelectObject(hdcSrc, handleRef);
					NativeMethods.BITMAP bITMAP = new NativeMethods.BITMAP();
					if (UnsafeNativeMethods.GetObject(handleRef, Marshal.SizeOf(bITMAP), bITMAP) == 0)
					{
						bool result = flag;
						return result;
					}
					NativeMethods.BITMAPINFO_FLAT bITMAPINFO_FLAT = default(NativeMethods.BITMAPINFO_FLAT);
					bITMAPINFO_FLAT.bmiHeader_biSize = Marshal.SizeOf(typeof(NativeMethods.BITMAPINFOHEADER));
					bITMAPINFO_FLAT.bmiHeader_biWidth = bITMAP.bmWidth;
					bITMAPINFO_FLAT.bmiHeader_biHeight = bITMAP.bmHeight;
					bITMAPINFO_FLAT.bmiHeader_biPlanes = 1;
					bITMAPINFO_FLAT.bmiHeader_biBitCount = bITMAP.bmBitsPixel;
					bITMAPINFO_FLAT.bmiHeader_biCompression = 0;
					bITMAPINFO_FLAT.bmiHeader_biSizeImage = 0;
					bITMAPINFO_FLAT.bmiHeader_biXPelsPerMeter = 0;
					bITMAPINFO_FLAT.bmiHeader_biYPelsPerMeter = 0;
					bITMAPINFO_FLAT.bmiHeader_biClrUsed = 0;
					bITMAPINFO_FLAT.bmiHeader_biClrImportant = 0;
					bITMAPINFO_FLAT.bmiColors = new byte[1024];
					long num = 1L << (int)(bITMAP.bmBitsPixel * bITMAP.bmPlanes & 31);
					if (num <= 256L)
					{
						byte[] array = new byte[Marshal.SizeOf(typeof(NativeMethods.PALETTEENTRY)) * 256];
						SafeNativeMethods.GetSystemPaletteEntries(hdcSrc, 0, (int)num, array);
						try
						{
							fixed (byte* ptr = bITMAPINFO_FLAT.bmiColors)
							{
								try
								{
									fixed (byte* ptr2 = array)
									{
										NativeMethods.RGBQUAD* ptr3 = (NativeMethods.RGBQUAD*)ptr;
										NativeMethods.PALETTEENTRY* ptr4 = (NativeMethods.PALETTEENTRY*)ptr2;
										for (long num2 = 0L; num2 < (long)((int)num); num2 += 1L)
										{
											ptr3[num2 * (long)sizeof(NativeMethods.RGBQUAD) / (long)sizeof(NativeMethods.RGBQUAD)].rgbRed = ptr4[num2 * (long)sizeof(NativeMethods.PALETTEENTRY) / (long)sizeof(NativeMethods.PALETTEENTRY)].peRed;
											ptr3[num2 * (long)sizeof(NativeMethods.RGBQUAD) / (long)sizeof(NativeMethods.RGBQUAD)].rgbBlue = ptr4[num2 * (long)sizeof(NativeMethods.PALETTEENTRY) / (long)sizeof(NativeMethods.PALETTEENTRY)].peBlue;
											ptr3[num2 * (long)sizeof(NativeMethods.RGBQUAD) / (long)sizeof(NativeMethods.RGBQUAD)].rgbGreen = ptr4[num2 * (long)sizeof(NativeMethods.PALETTEENTRY) / (long)sizeof(NativeMethods.PALETTEENTRY)].peGreen;
										}
									}
								}
								finally
								{
									byte* ptr2 = null;
								}
							}
						}
						finally
						{
							byte* ptr = null;
						}
					}
					byte[] array2 = new byte[((long)bITMAP.bmBitsPixel * (long)bITMAP.bmWidth + 7L) / 8L * (long)bITMAP.bmHeight];
					if (SafeNativeMethods.GetDIBits(hdcSrc, handleRef, 0, bITMAP.bmHeight, array2, ref bITMAPINFO_FLAT, 0) == 0)
					{
						bool result = flag;
						return result;
					}
					int left;
					int top;
					int nDestWidth;
					int nDestHeight;
					if (bStretch)
					{
						left = rect.left;
						top = rect.top;
						nDestWidth = rect.right - rect.left;
						nDestHeight = rect.bottom - rect.top;
					}
					else
					{
						left = rect.left;
						top = rect.top;
						nDestWidth = bITMAP.bmWidth;
						nDestHeight = bITMAP.bmHeight;
					}
					if (SafeNativeMethods.StretchDIBits(hdcDest, left, top, nDestWidth, nDestHeight, 0, 0, bITMAP.bmWidth, bITMAP.bmHeight, array2, ref bITMAPINFO_FLAT, 0, 13369376) == -1)
					{
						bool result = flag;
						return result;
					}
					flag = true;
				}
				finally
				{
					SafeNativeMethods.DeleteObject(hObject);
				}
				return flag;
			}
		}

		/// <summary>Содержит сведения об элементе управления, который может использоваться приложением, предоставляющим специальные возможности.</summary>
		[ComVisible(true)]
		public class ControlAccessibleObject : AccessibleObject
		{
			private static IntPtr oleAccAvailable = NativeMethods.InvalidIntPtr;

			private IntPtr handle = IntPtr.Zero;

			private Control ownerControl;

			/// <returns>Описание действия по умолчанию для объекта или значение null, если данный объект не имеет действия по умолчанию.</returns>
			public override string DefaultAction
			{
				get
				{
					string accessibleDefaultActionDescription = this.ownerControl.AccessibleDefaultActionDescription;
					if (accessibleDefaultActionDescription != null)
					{
						return accessibleDefaultActionDescription;
					}
					return base.DefaultAction;
				}
			}

			/// <summary>Получает описание <see cref="T:System.Windows.Forms.Control.ControlAccessibleObject" />.</summary>
			/// <returns>A string describing the <see cref="T:System.Windows.Forms.Control.ControlAccessibleObject" />.</returns>
			public override string Description
			{
				get
				{
					string accessibleDescription = this.ownerControl.AccessibleDescription;
					if (accessibleDescription != null)
					{
						return accessibleDescription;
					}
					return base.Description;
				}
			}

			/// <summary>Получает или задает дескриптор объекта специальных возможностей.</summary>
			/// <returns>Объект <see cref="T:System.IntPtr" />, который представляет собой дескриптор элемента управления.</returns>
			public IntPtr Handle
			{
				get
				{
					return this.handle;
				}
				set
				{
					IntSecurity.UnmanagedCode.Demand();
					if (this.handle != value)
					{
						this.handle = value;
						if (Control.ControlAccessibleObject.oleAccAvailable == IntPtr.Zero)
						{
							return;
						}
						bool flag = false;
						if (Control.ControlAccessibleObject.oleAccAvailable == NativeMethods.InvalidIntPtr)
						{
							Control.ControlAccessibleObject.oleAccAvailable = UnsafeNativeMethods.LoadLibrary("oleacc.dll");
							flag = (Control.ControlAccessibleObject.oleAccAvailable != IntPtr.Zero);
						}
						if (this.handle != IntPtr.Zero && Control.ControlAccessibleObject.oleAccAvailable != IntPtr.Zero)
						{
							base.UseStdAccessibleObjects(this.handle);
						}
						if (flag)
						{
							UnsafeNativeMethods.FreeLibrary(new HandleRef(null, Control.ControlAccessibleObject.oleAccAvailable));
						}
					}
				}
			}

			/// <summary>Получает описание действий, которые выполняет объект, и способов его применения.</summary>
			/// <returns>Описание действий, которые выполняет объект, и способов его применения.</returns>
			public override string Help
			{
				get
				{
					QueryAccessibilityHelpEventHandler queryAccessibilityHelpEventHandler = (QueryAccessibilityHelpEventHandler)this.Owner.Events[Control.EventQueryAccessibilityHelp];
					if (queryAccessibilityHelpEventHandler != null)
					{
						QueryAccessibilityHelpEventArgs queryAccessibilityHelpEventArgs = new QueryAccessibilityHelpEventArgs();
						queryAccessibilityHelpEventHandler(this.Owner, queryAccessibilityHelpEventArgs);
						return queryAccessibilityHelpEventArgs.HelpString;
					}
					return base.Help;
				}
			}

			/// <summary>Получает сочетание клавиш или назначенную клавишу для объекта специальных возможностей.</summary>
			/// <returns>Сочетание клавиш или клавиша доступа для объекта специальных возможностей, либо значение null, если отсутствует сочетание клавиш, связанное с этим объектом.</returns>
			public override string KeyboardShortcut
			{
				get
				{
					char mnemonic = WindowsFormsUtils.GetMnemonic(this.TextLabel, false);
					if (mnemonic != '\0')
					{
						return "Alt+" + mnemonic.ToString();
					}
					return null;
				}
			}

			/// <summary>Получает или задает имя объекта специальных возможностей.</summary>
			/// <returns>Имя объекта специальных возможностей.</returns>
			public override string Name
			{
				get
				{
					string accessibleName = this.ownerControl.AccessibleName;
					if (accessibleName != null)
					{
						return accessibleName;
					}
					return WindowsFormsUtils.TextWithoutMnemonics(this.TextLabel);
				}
				set
				{
					this.ownerControl.AccessibleName = value;
				}
			}

			/// <returns>Объект <see cref="T:System.Windows.Forms.AccessibleObject" />, представляющий родительский объект для объекта со специальными возможностями, или значение null, если родительский объект отсутствует.</returns>
			public override AccessibleObject Parent
			{
				[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode), SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
				get
				{
					return base.Parent;
				}
			}

			internal string TextLabel
			{
				get
				{
					if (this.ownerControl.GetStyle(ControlStyles.UseTextForAccessibility))
					{
						string text = this.ownerControl.Text;
						if (!string.IsNullOrEmpty(text))
						{
							return text;
						}
					}
					Label previousLabel = this.PreviousLabel;
					if (previousLabel != null)
					{
						string text2 = previousLabel.Text;
						if (!string.IsNullOrEmpty(text2))
						{
							return text2;
						}
					}
					return null;
				}
			}

			/// <summary>Получает владельца объекта специальных возможностей.</summary>
			/// <returns>Объект <see cref="T:System.Windows.Forms.Control" />, которому принадлежит <see cref="T:System.Windows.Forms.Control.ControlAccessibleObject" />.</returns>
			public Control Owner
			{
				get
				{
					return this.ownerControl;
				}
			}

			internal Label PreviousLabel
			{
				get
				{
					Control parentInternal = this.Owner.ParentInternal;
					if (parentInternal == null)
					{
						return null;
					}
					ContainerControl containerControl = parentInternal.GetContainerControlInternal() as ContainerControl;
					if (containerControl == null)
					{
						return null;
					}
					for (Control nextControl = containerControl.GetNextControl(this.Owner, false); nextControl != null; nextControl = containerControl.GetNextControl(nextControl, false))
					{
						if (nextControl is Label)
						{
							return nextControl as Label;
						}
						if (nextControl.Visible && nextControl.TabStop)
						{
							break;
						}
					}
					return null;
				}
			}

			/// <summary>Получает роль данного объекта со специальными возможностями.</summary>
			/// <returns>One of the <see cref="T:System.Windows.Forms.AccessibleRole" /> values.</returns>
			public override AccessibleRole Role
			{
				get
				{
					AccessibleRole accessibleRole = this.ownerControl.AccessibleRole;
					if (accessibleRole != AccessibleRole.Default)
					{
						return accessibleRole;
					}
					return base.Role;
				}
			}

			/// <summary>Инициализирует новый экземпляр класса <see cref="T:System.Windows.Forms.Control.ControlAccessibleObject" />.</summary>
			/// <param name="ownerControl">Объект <see cref="T:System.Windows.Forms.Control" />, которому принадлежит <see cref="T:System.Windows.Forms.Control.ControlAccessibleObject" />. </param>
			/// <exception cref="T:System.ArgumentNullException">The <paramref name="ownerControl" /> parameter value is null. </exception>
			public ControlAccessibleObject(Control ownerControl)
			{
				if (ownerControl == null)
				{
					throw new ArgumentNullException("ownerControl");
				}
				this.ownerControl = ownerControl;
				IntPtr intPtr = ownerControl.Handle;
				IntSecurity.UnmanagedCode.Assert();
				try
				{
					this.Handle = intPtr;
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
				}
			}

			internal ControlAccessibleObject(Control ownerControl, int accObjId)
			{
				if (ownerControl == null)
				{
					throw new ArgumentNullException("ownerControl");
				}
				base.AccessibleObjectId = accObjId;
				this.ownerControl = ownerControl;
				IntPtr intPtr = ownerControl.Handle;
				IntSecurity.UnmanagedCode.Assert();
				try
				{
					this.Handle = intPtr;
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
				}
			}

			internal override int[] GetSysChildOrder()
			{
				if (this.ownerControl.GetStyle(ControlStyles.ContainerControl))
				{
					return this.ownerControl.GetChildWindowsInTabOrder();
				}
				return base.GetSysChildOrder();
			}

			internal override bool GetSysChild(AccessibleNavigation navdir, out AccessibleObject accessibleObject)
			{
				accessibleObject = null;
				Control parentInternal = this.ownerControl.ParentInternal;
				int num = -1;
				Control[] array = null;
				switch (navdir)
				{
				case AccessibleNavigation.Next:
					if (base.IsNonClientObject && parentInternal != null)
					{
						array = parentInternal.GetChildControlsInTabOrder(true);
						num = Array.IndexOf<Control>(array, this.ownerControl);
						if (num != -1)
						{
							num++;
						}
					}
					break;
				case AccessibleNavigation.Previous:
					if (base.IsNonClientObject && parentInternal != null)
					{
						array = parentInternal.GetChildControlsInTabOrder(true);
						num = Array.IndexOf<Control>(array, this.ownerControl);
						if (num != -1)
						{
							num--;
						}
					}
					break;
				case AccessibleNavigation.FirstChild:
					if (base.IsClientObject)
					{
						array = this.ownerControl.GetChildControlsInTabOrder(true);
						num = 0;
					}
					break;
				case AccessibleNavigation.LastChild:
					if (base.IsClientObject)
					{
						array = this.ownerControl.GetChildControlsInTabOrder(true);
						num = array.Length - 1;
					}
					break;
				}
				if (array == null || array.Length == 0)
				{
					return false;
				}
				if (num >= 0 && num < array.Length)
				{
					accessibleObject = array[num].NcAccessibilityObject;
				}
				return true;
			}

			/// <summary>Получает идентификатор раздела справки и путь к файлу справки, который связан с объектом специальных возможностей.</summary>
			/// <returns>Идентификатор для раздела справки или значение -1, если раздел справки отсутствует.Возвращаемый параметр <paramref name="fileName" />  содержит путь к файлу справки, связанному с данным объектом со специальными возможностями, или значение null, если интерфейс IAccessible не задан.</returns>
			/// <param name="fileName">Значение, возвращаемое этим методом, содержит строку, которая представляет путь к файлу справки, связанному с этим объектом со специальными возможностями.Этот параметр передается без инициализации.</param>
			public override int GetHelpTopic(out string fileName)
			{
				int result = 0;
				QueryAccessibilityHelpEventHandler queryAccessibilityHelpEventHandler = (QueryAccessibilityHelpEventHandler)this.Owner.Events[Control.EventQueryAccessibilityHelp];
				if (queryAccessibilityHelpEventHandler != null)
				{
					QueryAccessibilityHelpEventArgs queryAccessibilityHelpEventArgs = new QueryAccessibilityHelpEventArgs();
					queryAccessibilityHelpEventHandler(this.Owner, queryAccessibilityHelpEventArgs);
					fileName = queryAccessibilityHelpEventArgs.HelpNamespace;
					if (!string.IsNullOrEmpty(fileName))
					{
						IntSecurity.DemandFileIO(FileIOPermissionAccess.PathDiscovery, fileName);
					}
					try
					{
						result = int.Parse(queryAccessibilityHelpEventArgs.HelpKeyword, CultureInfo.InvariantCulture);
					}
					catch (Exception arg_60_0)
					{
						if (ClientUtils.IsSecurityOrCriticalException(arg_60_0))
						{
							throw;
						}
					}
					return result;
				}
				return base.GetHelpTopic(out fileName);
			}

			/// <summary>Сообщает клиентским приложениям со специальными возможностями об указанных событиях <see cref="T:System.Windows.Forms.AccessibleEvents" />.</summary>
			/// <param name="accEvent">Перечисление <see cref="T:System.Windows.Forms.AccessibleEvents" />, о котором требуется уведомлять клиентские приложения со специальными возможностями. </param>
			public void NotifyClients(AccessibleEvents accEvent)
			{
				UnsafeNativeMethods.NotifyWinEvent((int)accEvent, new HandleRef(this, this.Handle), -4, 0);
			}

			/// <summary>Уведомляет клиентские приложения со специальными возможностями об указанном перечислении <see cref="T:System.Windows.Forms.AccessibleEvents" /> для указанного дочернего элемента управления.</summary>
			/// <param name="accEvent">Перечисление <see cref="T:System.Windows.Forms.AccessibleEvents" />, о котором требуется уведомлять клиентские приложения со специальными возможностями. </param>
			/// <param name="childID">Дочерний объект <see cref="T:System.Windows.Forms.Control" />, который требуется уведомлять о событии, связанном со специальными возможностями. </param>
			public void NotifyClients(AccessibleEvents accEvent, int childID)
			{
				UnsafeNativeMethods.NotifyWinEvent((int)accEvent, new HandleRef(this, this.Handle), -4, childID + 1);
			}

			/// <summary>Уведомляет клиентские приложения со специальными возможностями об указанном перечислении <see cref="T:System.Windows.Forms.AccessibleEvents" /> для указанного дочернего элемента управления, предоставляя идентификацию объекта <see cref="T:System.Windows.Forms.AccessibleObject" />.</summary>
			/// <param name="accEvent">Перечисление <see cref="T:System.Windows.Forms.AccessibleEvents" />, о котором требуется уведомлять клиентские приложения со специальными возможностями.</param>
			/// <param name="objectID">Идентификатор объекта <see cref="T:System.Windows.Forms.AccessibleObject" />.</param>
			/// <param name="childID">Дочерний объект <see cref="T:System.Windows.Forms.Control" />, который требуется уведомлять о событии, связанном со специальными возможностями.</param>
			public void NotifyClients(AccessibleEvents accEvent, int objectID, int childID)
			{
				UnsafeNativeMethods.NotifyWinEvent((int)accEvent, new HandleRef(this, this.Handle), objectID, childID + 1);
			}

			/// <returns>Объект <see cref="T:System.String" />, представляющий текущий объект <see cref="T:System.Object" />.</returns>
			public override string ToString()
			{
				if (this.Owner != null)
				{
					return "ControlAccessibleObject: Owner = " + this.Owner.ToString();
				}
				return "ControlAccessibleObject: Owner = null";
			}
		}

		internal sealed class FontHandleWrapper : MarshalByRefObject, IDisposable
		{
			private IntPtr handle;

			internal IntPtr Handle
			{
				get
				{
					return this.handle;
				}
			}

			internal FontHandleWrapper(Font font)
			{
				this.handle = font.ToHfont();
				System.Internal.HandleCollector.Add(this.handle, NativeMethods.CommonHandles.GDI);
			}

			public void Dispose()
			{
				this.Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				if (this.handle != IntPtr.Zero)
				{
					SafeNativeMethods.DeleteObject(new HandleRef(this, this.handle));
					this.handle = IntPtr.Zero;
				}
			}

			~FontHandleWrapper()
			{
				this.Dispose(false);
			}
		}

		private class ThreadMethodEntry : IAsyncResult
		{
			internal Control caller;

			internal Control marshaler;

			internal Delegate method;

			internal object[] args;

			internal object retVal;

			internal Exception exception;

			internal bool synchronous;

			private bool isCompleted;

			private ManualResetEvent resetEvent;

			private object invokeSyncObject = new object();

			internal ExecutionContext executionContext;

			internal SynchronizationContext syncContext;

			public object AsyncState
			{
				get
				{
					return null;
				}
			}

			public WaitHandle AsyncWaitHandle
			{
				get
				{
					if (this.resetEvent == null)
					{
						object obj = this.invokeSyncObject;
						lock (obj)
						{
							if (this.resetEvent == null)
							{
								this.resetEvent = new ManualResetEvent(false);
								if (this.isCompleted)
								{
									this.resetEvent.Set();
								}
							}
						}
					}
					return this.resetEvent;
				}
			}

			public bool CompletedSynchronously
			{
				get
				{
					return this.isCompleted && this.synchronous;
				}
			}

			public bool IsCompleted
			{
				get
				{
					return this.isCompleted;
				}
			}

			internal ThreadMethodEntry(Control caller, Control marshaler, Delegate method, object[] args, bool synchronous, ExecutionContext executionContext)
			{
				this.caller = caller;
				this.marshaler = marshaler;
				this.method = method;
				this.args = args;
				this.exception = null;
				this.retVal = null;
				this.synchronous = synchronous;
				this.isCompleted = false;
				this.resetEvent = null;
				this.executionContext = executionContext;
			}

			~ThreadMethodEntry()
			{
				if (this.resetEvent != null)
				{
					this.resetEvent.Close();
				}
			}

			internal void Complete()
			{
				object obj = this.invokeSyncObject;
				lock (obj)
				{
					this.isCompleted = true;
					if (this.resetEvent != null)
					{
						this.resetEvent.Set();
					}
				}
			}
		}

		private class ControlVersionInfo
		{
			private string companyName;

			private string productName;

			private string productVersion;

			private FileVersionInfo versionInfo;

			private Control owner;

			internal string CompanyName
			{
				get
				{
					if (this.companyName == null)
					{
						object[] customAttributes = this.owner.GetType().Module.Assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
						if (customAttributes != null && customAttributes.Length != 0)
						{
							this.companyName = ((AssemblyCompanyAttribute)customAttributes[0]).Company;
						}
						if (this.companyName == null || this.companyName.Length == 0)
						{
							this.companyName = this.GetFileVersionInfo().CompanyName;
							if (this.companyName != null)
							{
								this.companyName = this.companyName.Trim();
							}
						}
						if (this.companyName == null || this.companyName.Length == 0)
						{
							string text = this.owner.GetType().Namespace;
							if (text == null)
							{
								text = string.Empty;
							}
							int num = text.IndexOf("/");
							if (num != -1)
							{
								this.companyName = text.Substring(0, num);
							}
							else
							{
								this.companyName = text;
							}
						}
					}
					return this.companyName;
				}
			}

			internal string ProductName
			{
				get
				{
					if (this.productName == null)
					{
						object[] customAttributes = this.owner.GetType().Module.Assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
						if (customAttributes != null && customAttributes.Length != 0)
						{
							this.productName = ((AssemblyProductAttribute)customAttributes[0]).Product;
						}
						if (this.productName == null || this.productName.Length == 0)
						{
							this.productName = this.GetFileVersionInfo().ProductName;
							if (this.productName != null)
							{
								this.productName = this.productName.Trim();
							}
						}
						if (this.productName == null || this.productName.Length == 0)
						{
							string text = this.owner.GetType().Namespace;
							if (text == null)
							{
								text = string.Empty;
							}
							int num = text.IndexOf(".");
							if (num != -1)
							{
								this.productName = text.Substring(num + 1);
							}
							else
							{
								this.productName = text;
							}
						}
					}
					return this.productName;
				}
			}

			internal string ProductVersion
			{
				get
				{
					if (this.productVersion == null)
					{
						object[] customAttributes = this.owner.GetType().Module.Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
						if (customAttributes != null && customAttributes.Length != 0)
						{
							this.productVersion = ((AssemblyInformationalVersionAttribute)customAttributes[0]).InformationalVersion;
						}
						if (this.productVersion == null || this.productVersion.Length == 0)
						{
							this.productVersion = this.GetFileVersionInfo().ProductVersion;
							if (this.productVersion != null)
							{
								this.productVersion = this.productVersion.Trim();
							}
						}
						if (this.productVersion == null || this.productVersion.Length == 0)
						{
							this.productVersion = "1.0.0.0";
						}
					}
					return this.productVersion;
				}
			}

			internal ControlVersionInfo(Control owner)
			{
				this.owner = owner;
			}

			private FileVersionInfo GetFileVersionInfo()
			{
				if (this.versionInfo == null)
				{
					new FileIOPermission(PermissionState.None)
					{
						AllFiles = FileIOPermissionAccess.PathDiscovery
					}.Assert();
					string fullyQualifiedName;
					try
					{
						fullyQualifiedName = this.owner.GetType().Module.FullyQualifiedName;
					}
					finally
					{
						CodeAccessPermission.RevertAssert();
					}
					new FileIOPermission(FileIOPermissionAccess.Read, fullyQualifiedName).Assert();
					try
					{
						this.versionInfo = FileVersionInfo.GetVersionInfo(fullyQualifiedName);
					}
					finally
					{
						CodeAccessPermission.RevertAssert();
					}
				}
				return this.versionInfo;
			}
		}

		private sealed class MultithreadSafeCallScope : IDisposable
		{
			private bool resultedInSet;

			internal MultithreadSafeCallScope()
			{
				if (Control.checkForIllegalCrossThreadCalls && !Control.inCrossThreadSafeCall)
				{
					Control.inCrossThreadSafeCall = true;
					this.resultedInSet = true;
					return;
				}
				this.resultedInSet = false;
			}

			void IDisposable.Dispose()
			{
				if (this.resultedInSet)
				{
					Control.inCrossThreadSafeCall = false;
				}
			}
		}

		private sealed class PrintPaintEventArgs : PaintEventArgs
		{
			private Message m;

			internal Message Message
			{
				get
				{
					return this.m;
				}
			}

			internal PrintPaintEventArgs(Message m, IntPtr dc, Rectangle clipRect) : base(dc, clipRect)
			{
				this.m = m;
			}
		}

		internal static readonly TraceSwitch ControlKeyboardRouting;

		internal static readonly TraceSwitch PaletteTracing;

		internal static readonly TraceSwitch FocusTracing;

		internal static readonly BooleanSwitch BufferPinkRect;

		private static int WM_GETCONTROLNAME;

		private static int WM_GETCONTROLTYPE;

		internal const int STATE_CREATED = 1;

		internal const int STATE_VISIBLE = 2;

		internal const int STATE_ENABLED = 4;

		internal const int STATE_TABSTOP = 8;

		internal const int STATE_RECREATE = 16;

		internal const int STATE_MODAL = 32;

		internal const int STATE_ALLOWDROP = 64;

		internal const int STATE_DROPTARGET = 128;

		internal const int STATE_NOZORDER = 256;

		internal const int STATE_LAYOUTDEFERRED = 512;

		internal const int STATE_USEWAITCURSOR = 1024;

		internal const int STATE_DISPOSED = 2048;

		internal const int STATE_DISPOSING = 4096;

		internal const int STATE_MOUSEENTERPENDING = 8192;

		internal const int STATE_TRACKINGMOUSEEVENT = 16384;

		internal const int STATE_THREADMARSHALLPENDING = 32768;

		internal const int STATE_SIZELOCKEDBYOS = 65536;

		internal const int STATE_CAUSESVALIDATION = 131072;

		internal const int STATE_CREATINGHANDLE = 262144;

		internal const int STATE_TOPLEVEL = 524288;

		internal const int STATE_ISACCESSIBLE = 1048576;

		internal const int STATE_OWNCTLBRUSH = 2097152;

		internal const int STATE_EXCEPTIONWHILEPAINTING = 4194304;

		internal const int STATE_LAYOUTISDIRTY = 8388608;

		internal const int STATE_CHECKEDHOST = 16777216;

		internal const int STATE_HOSTEDINDIALOG = 33554432;

		internal const int STATE_DOUBLECLICKFIRED = 67108864;

		internal const int STATE_MOUSEPRESSED = 134217728;

		internal const int STATE_VALIDATIONCANCELLED = 268435456;

		internal const int STATE_PARENTRECREATING = 536870912;

		internal const int STATE_MIRRORED = 1073741824;

		private const int STATE2_HAVEINVOKED = 1;

		private const int STATE2_SETSCROLLPOS = 2;

		private const int STATE2_LISTENINGTOUSERPREFERENCECHANGED = 4;

		internal const int STATE2_INTERESTEDINUSERPREFERENCECHANGED = 8;

		internal const int STATE2_MAINTAINSOWNCAPTUREMODE = 16;

		private const int STATE2_BECOMINGACTIVECONTROL = 32;

		private const int STATE2_CLEARLAYOUTARGS = 64;

		private const int STATE2_INPUTKEY = 128;

		private const int STATE2_INPUTCHAR = 256;

		private const int STATE2_UICUES = 512;

		private const int STATE2_ISACTIVEX = 1024;

		internal const int STATE2_USEPREFERREDSIZECACHE = 2048;

		internal const int STATE2_TOPMDIWINDOWCLOSING = 4096;

		private static readonly object EventAutoSizeChanged;

		private static readonly object EventKeyDown;

		private static readonly object EventKeyPress;

		private static readonly object EventKeyUp;

		private static readonly object EventMouseDown;

		private static readonly object EventMouseEnter;

		private static readonly object EventMouseLeave;

		private static readonly object EventMouseHover;

		private static readonly object EventMouseMove;

		private static readonly object EventMouseUp;

		private static readonly object EventMouseWheel;

		private static readonly object EventClick;

		private static readonly object EventClientSize;

		private static readonly object EventDoubleClick;

		private static readonly object EventMouseClick;

		private static readonly object EventMouseDoubleClick;

		private static readonly object EventMouseCaptureChanged;

		private static readonly object EventMove;

		private static readonly object EventResize;

		private static readonly object EventLayout;

		private static readonly object EventGotFocus;

		private static readonly object EventLostFocus;

		private static readonly object EventEnabledChanged;

		private static readonly object EventEnter;

		private static readonly object EventLeave;

		private static readonly object EventHandleCreated;

		private static readonly object EventHandleDestroyed;

		private static readonly object EventVisibleChanged;

		private static readonly object EventControlAdded;

		private static readonly object EventControlRemoved;

		private static readonly object EventChangeUICues;

		private static readonly object EventSystemColorsChanged;

		private static readonly object EventValidating;

		private static readonly object EventValidated;

		private static readonly object EventStyleChanged;

		private static readonly object EventImeModeChanged;

		private static readonly object EventHelpRequested;

		private static readonly object EventPaint;

		private static readonly object EventInvalidated;

		private static readonly object EventQueryContinueDrag;

		private static readonly object EventGiveFeedback;

		private static readonly object EventDragEnter;

		private static readonly object EventDragLeave;

		private static readonly object EventDragOver;

		private static readonly object EventDragDrop;

		private static readonly object EventQueryAccessibilityHelp;

		private static readonly object EventBackgroundImage;

		private static readonly object EventBackgroundImageLayout;

		private static readonly object EventBindingContext;

		private static readonly object EventBackColor;

		private static readonly object EventParent;

		private static readonly object EventVisible;

		private static readonly object EventText;

		private static readonly object EventTabStop;

		private static readonly object EventTabIndex;

		private static readonly object EventSize;

		private static readonly object EventRightToLeft;

		private static readonly object EventLocation;

		private static readonly object EventForeColor;

		private static readonly object EventFont;

		private static readonly object EventEnabled;

		private static readonly object EventDock;

		private static readonly object EventCursor;

		private static readonly object EventContextMenu;

		private static readonly object EventContextMenuStrip;

		private static readonly object EventCausesValidation;

		private static readonly object EventRegionChanged;

		private static readonly object EventMarginChanged;

		internal static readonly object EventPaddingChanged;

		private static readonly object EventPreviewKeyDown;

		private static int mouseWheelMessage;

		private static bool mouseWheelRoutingNeeded;

		private static bool mouseWheelInit;

		private static int threadCallbackMessage;

		private static bool checkForIllegalCrossThreadCalls;

		private static ContextCallback invokeMarshaledCallbackHelperDelegate;

		[ThreadStatic]
		private static bool inCrossThreadSafeCall;

		[ThreadStatic]
		internal static HelpInfo currentHelpInfo;

		private static Control.FontHandleWrapper defaultFontHandleWrapper;

		private const short PaintLayerBackground = 1;

		private const short PaintLayerForeground = 2;

		private const byte RequiredScalingEnabledMask = 16;

		private const byte RequiredScalingMask = 15;

		private static Font defaultFont;

		private static readonly int PropName;

		private static readonly int PropBackBrush;

		private static readonly int PropFontHeight;

		private static readonly int PropCurrentAmbientFont;

		private static readonly int PropControlsCollection;

		private static readonly int PropBackColor;

		private static readonly int PropForeColor;

		private static readonly int PropFont;

		private static readonly int PropBackgroundImage;

		private static readonly int PropFontHandleWrapper;

		private static readonly int PropUserData;

		private static readonly int PropContextMenu;

		private static readonly int PropCursor;

		private static readonly int PropRegion;

		private static readonly int PropRightToLeft;

		private static readonly int PropBindings;

		private static readonly int PropBindingManager;

		private static readonly int PropAccessibleDefaultActionDescription;

		private static readonly int PropAccessibleDescription;

		private static readonly int PropAccessibility;

		private static readonly int PropNcAccessibility;

		private static readonly int PropAccessibleName;

		private static readonly int PropAccessibleRole;

		private static readonly int PropPaintingException;

		private static readonly int PropActiveXImpl;

		private static readonly int PropControlVersionInfo;

		private static readonly int PropBackgroundImageLayout;

		private static readonly int PropAccessibleHelpProvider;

		private static readonly int PropContextMenuStrip;

		private static readonly int PropAutoScrollOffset;

		private static readonly int PropUseCompatibleTextRendering;

		private static readonly int PropImeWmCharsToIgnore;

		private static readonly int PropImeMode;

		private static readonly int PropDisableImeModeChangedCount;

		private static readonly int PropLastCanEnableIme;

		private static readonly int PropCacheTextCount;

		private static readonly int PropCacheTextField;

		private static readonly int PropAmbientPropertiesService;

		internal static bool UseCompatibleTextRenderingDefault;

		private Control.ControlNativeWindow window;

		private Control parent;

		private Control reflectParent;

		private CreateParams createParams;

		private int x;

		private int y;

		private int width;

		private int height;

		private int clientWidth;

		private int clientHeight;

		private int state;

		private int state2;

		private ControlStyles controlStyle;

		private int tabIndex;

		private string text;

		private byte layoutSuspendCount;

		private byte requiredScaling;

		private PropertyStore propertyStore;

		private NativeMethods.TRACKMOUSEEVENT trackMouseEvent;

		private short updateCount;

		private LayoutEventArgs cachedLayoutEventArgs;

		private Queue threadCallbackList;

		private int uiCuesState;

		private const int UISTATE_FOCUS_CUES_MASK = 15;

		private const int UISTATE_FOCUS_CUES_HIDDEN = 1;

		private const int UISTATE_FOCUS_CUES_SHOW = 2;

		private const int UISTATE_KEYBOARD_CUES_MASK = 240;

		private const int UISTATE_KEYBOARD_CUES_HIDDEN = 16;

		private const int UISTATE_KEYBOARD_CUES_SHOW = 32;

		private const int ImeCharsToIgnoreDisabled = -1;

		private const int ImeCharsToIgnoreEnabled = 0;

		private static ImeMode propagatingImeMode;

		private static bool ignoreWmImeNotify;

		private static bool lastLanguageChinese;

		/// <summary>Данное событие не относится к этому классу.</summary>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Never), SRCategory("CatPropertyChanged"), SRDescription("ControlOnAutoSizeChangedDescr")]
		public event EventHandler AutoSizeChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventAutoSizeChanged, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventAutoSizeChanged, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.BackColor" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnBackColorChangedDescr")]
		public event EventHandler BackColorChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventBackColor, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventBackColor, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.BackgroundImage" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnBackgroundImageChangedDescr")]
		public event EventHandler BackgroundImageChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventBackgroundImage, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventBackgroundImage, value);
			}
		}

		/// <summary>Происходит при изменении свойства <see cref="P:System.Windows.Forms.Control.BackgroundImageLayout" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnBackgroundImageLayoutChangedDescr")]
		public event EventHandler BackgroundImageLayoutChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventBackgroundImageLayout, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventBackgroundImageLayout, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="T:System.Windows.Forms.BindingContext" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnBindingContextChangedDescr")]
		public event EventHandler BindingContextChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventBindingContext, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventBindingContext, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.CausesValidation" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnCausesValidationChangedDescr")]
		public event EventHandler CausesValidationChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventCausesValidation, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventCausesValidation, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.ClientSize" />. </summary>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnClientSizeChangedDescr")]
		public event EventHandler ClientSizeChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventClientSize, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventClientSize, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.ContextMenu" />.</summary>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), SRCategory("CatPropertyChanged"), SRDescription("ControlOnContextMenuChangedDescr")]
		public event EventHandler ContextMenuChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventContextMenu, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventContextMenu, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.ContextMenuStrip" />. </summary>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlContextMenuStripChangedDescr")]
		public event EventHandler ContextMenuStripChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventContextMenuStrip, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventContextMenuStrip, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.Cursor" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnCursorChangedDescr")]
		public event EventHandler CursorChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventCursor, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventCursor, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.Dock" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnDockChangedDescr")]
		public event EventHandler DockChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventDock, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventDock, value);
			}
		}

		/// <summary>Происходит, если значение свойства <see cref="P:System.Windows.Forms.Control.Enabled" /> было изменено.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnEnabledChangedDescr")]
		public event EventHandler EnabledChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventEnabled, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventEnabled, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.Font" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnFontChangedDescr")]
		public event EventHandler FontChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventFont, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventFont, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.ForeColor" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnForeColorChangedDescr")]
		public event EventHandler ForeColorChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventForeColor, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventForeColor, value);
			}
		}

		/// <summary>Происходит, если значение свойства <see cref="P:System.Windows.Forms.Control.Location" /> было изменено.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnLocationChangedDescr")]
		public event EventHandler LocationChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventLocation, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventLocation, value);
			}
		}

		/// <summary>Происходит при изменении поля элемента управления.</summary>
		[SRCategory("CatLayout"), SRDescription("ControlOnMarginChangedDescr")]
		public event EventHandler MarginChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventMarginChanged, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventMarginChanged, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.Region" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlRegionChangedDescr")]
		public event EventHandler RegionChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventRegionChanged, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventRegionChanged, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.RightToLeft" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnRightToLeftChangedDescr")]
		public event EventHandler RightToLeftChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventRightToLeft, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventRightToLeft, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.Size" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnSizeChangedDescr")]
		public event EventHandler SizeChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventSize, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventSize, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.TabIndex" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnTabIndexChangedDescr")]
		public event EventHandler TabIndexChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventTabIndex, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventTabIndex, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.TabStop" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnTabStopChangedDescr")]
		public event EventHandler TabStopChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventTabStop, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventTabStop, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.Text" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnTextChangedDescr")]
		public event EventHandler TextChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventText, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventText, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.Visible" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnVisibleChangedDescr")]
		public event EventHandler VisibleChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventVisible, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventVisible, value);
			}
		}

		/// <summary>Происходит при щелчке элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatAction"), SRDescription("ControlOnClickDescr")]
		public event EventHandler Click
		{
			add
			{
				base.Events.AddHandler(Control.EventClick, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventClick, value);
			}
		}

		/// <summary>Происходит при добавлении нового элемента управления к коллекции <see cref="T:System.Windows.Forms.Control.ControlCollection" />.</summary>
		/// <filterpriority>1</filterpriority>
		[Browsable(true), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatBehavior"), SRDescription("ControlOnControlAddedDescr")]
		public event ControlEventHandler ControlAdded
		{
			add
			{
				base.Events.AddHandler(Control.EventControlAdded, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventControlAdded, value);
			}
		}

		/// <summary>Происходит при удалении элемента управления из коллекции <see cref="T:System.Windows.Forms.Control.ControlCollection" />.</summary>
		/// <filterpriority>1</filterpriority>
		[Browsable(true), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatBehavior"), SRDescription("ControlOnControlRemovedDescr")]
		public event ControlEventHandler ControlRemoved
		{
			add
			{
				base.Events.AddHandler(Control.EventControlRemoved, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventControlRemoved, value);
			}
		}

		/// <summary>Происходит по завершении операции перетаскивания.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatDragDrop"), SRDescription("ControlOnDragDropDescr")]
		public event DragEventHandler DragDrop
		{
			add
			{
				base.Events.AddHandler(Control.EventDragDrop, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventDragDrop, value);
			}
		}

		/// <summary>Происходит при перетаскивании объекта в пределы элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatDragDrop"), SRDescription("ControlOnDragEnterDescr")]
		public event DragEventHandler DragEnter
		{
			add
			{
				base.Events.AddHandler(Control.EventDragEnter, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventDragEnter, value);
			}
		}

		/// <summary>Происходит, когда объект перетаскивается через границу элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatDragDrop"), SRDescription("ControlOnDragOverDescr")]
		public event DragEventHandler DragOver
		{
			add
			{
				base.Events.AddHandler(Control.EventDragOver, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventDragOver, value);
			}
		}

		/// <summary>Происходит при перетаскивании объекта за пределы элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatDragDrop"), SRDescription("ControlOnDragLeaveDescr")]
		public event EventHandler DragLeave
		{
			add
			{
				base.Events.AddHandler(Control.EventDragLeave, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventDragLeave, value);
			}
		}

		/// <summary>Происходит при выполнении операции перетаскивания.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatDragDrop"), SRDescription("ControlOnGiveFeedbackDescr")]
		public event GiveFeedbackEventHandler GiveFeedback
		{
			add
			{
				base.Events.AddHandler(Control.EventGiveFeedback, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventGiveFeedback, value);
			}
		}

		/// <summary>Происходит при создании дескриптора для элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatPrivate"), SRDescription("ControlOnCreateHandleDescr")]
		public event EventHandler HandleCreated
		{
			add
			{
				base.Events.AddHandler(Control.EventHandleCreated, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventHandleCreated, value);
			}
		}

		/// <summary>Происходит в процессе удаления дескриптора элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatPrivate"), SRDescription("ControlOnDestroyHandleDescr")]
		public event EventHandler HandleDestroyed
		{
			add
			{
				base.Events.AddHandler(Control.EventHandleDestroyed, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventHandleDestroyed, value);
			}
		}

		/// <summary>Происходит при запросе справки для элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatBehavior"), SRDescription("ControlOnHelpDescr")]
		public event HelpEventHandler HelpRequested
		{
			add
			{
				base.Events.AddHandler(Control.EventHelpRequested, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventHelpRequested, value);
			}
		}

		/// <summary>Происходит, когда требуется перерисовать отображение элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatAppearance"), SRDescription("ControlOnInvalidateDescr")]
		public event InvalidateEventHandler Invalidated
		{
			add
			{
				base.Events.AddHandler(Control.EventInvalidated, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventInvalidated, value);
			}
		}

		/// <summary>Генерируется при изменении заполнения элемента управления.</summary>
		[SRCategory("CatLayout"), SRDescription("ControlOnPaddingChangedDescr")]
		public event EventHandler PaddingChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventPaddingChanged, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventPaddingChanged, value);
			}
		}

		/// <summary>Происходит при перерисовке элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatAppearance"), SRDescription("ControlOnPaintDescr")]
		public event PaintEventHandler Paint
		{
			add
			{
				base.Events.AddHandler(Control.EventPaint, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventPaint, value);
			}
		}

		/// <summary>Происходит во время операции перетаскивания и позволяет источнику перетаскивания определить, следует ли отменить эту операцию.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatDragDrop"), SRDescription("ControlOnQueryContinueDragDescr")]
		public event QueryContinueDragEventHandler QueryContinueDrag
		{
			add
			{
				base.Events.AddHandler(Control.EventQueryContinueDrag, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventQueryContinueDrag, value);
			}
		}

		/// <summary>Происходит, когда объект <see cref="T:System.Windows.Forms.AccessibleObject" /> предоставляет справку для приложений со специальными возможностями.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatBehavior"), SRDescription("ControlOnQueryAccessibilityHelpDescr")]
		public event QueryAccessibilityHelpEventHandler QueryAccessibilityHelp
		{
			add
			{
				base.Events.AddHandler(Control.EventQueryAccessibilityHelp, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventQueryAccessibilityHelp, value);
			}
		}

		/// <summary>Происходит, когда элемент управления дважды щелкается.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatAction"), SRDescription("ControlOnDoubleClickDescr")]
		public event EventHandler DoubleClick
		{
			add
			{
				base.Events.AddHandler(Control.EventDoubleClick, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventDoubleClick, value);
			}
		}

		/// <summary>Происходит при входе в элемент управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatFocus"), SRDescription("ControlOnEnterDescr")]
		public event EventHandler Enter
		{
			add
			{
				base.Events.AddHandler(Control.EventEnter, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventEnter, value);
			}
		}

		/// <summary>Генерируется при получении фокуса элементом управления.</summary>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatFocus"), SRDescription("ControlOnGotFocusDescr")]
		public event EventHandler GotFocus
		{
			add
			{
				base.Events.AddHandler(Control.EventGotFocus, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventGotFocus, value);
			}
		}

		/// <summary>Происходит при нажатии клавиши, если элемент управления имеет фокус.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatKey"), SRDescription("ControlOnKeyDownDescr")]
		public event KeyEventHandler KeyDown
		{
			add
			{
				base.Events.AddHandler(Control.EventKeyDown, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventKeyDown, value);
			}
		}

		/// <summary>Происходит при нажатии клавиши, если элемент управления имеет фокус.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatKey"), SRDescription("ControlOnKeyPressDescr")]
		public event KeyPressEventHandler KeyPress
		{
			add
			{
				base.Events.AddHandler(Control.EventKeyPress, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventKeyPress, value);
			}
		}

		/// <summary>Происходит, когда отпускается клавиша, если элемент управления имеет фокус.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatKey"), SRDescription("ControlOnKeyUpDescr")]
		public event KeyEventHandler KeyUp
		{
			add
			{
				base.Events.AddHandler(Control.EventKeyUp, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventKeyUp, value);
			}
		}

		/// <summary>Происходит, когда необходимо изменить позицию дочерних элементов управления данного элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatLayout"), SRDescription("ControlOnLayoutDescr")]
		public event LayoutEventHandler Layout
		{
			add
			{
				base.Events.AddHandler(Control.EventLayout, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventLayout, value);
			}
		}

		/// <summary>Происходит, когда фокус ввода покидает элемент управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatFocus"), SRDescription("ControlOnLeaveDescr")]
		public event EventHandler Leave
		{
			add
			{
				base.Events.AddHandler(Control.EventLeave, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventLeave, value);
			}
		}

		/// <summary>Генерируется при потере фокуса элементом управления.</summary>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatFocus"), SRDescription("ControlOnLostFocusDescr")]
		public event EventHandler LostFocus
		{
			add
			{
				base.Events.AddHandler(Control.EventLostFocus, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventLostFocus, value);
			}
		}

		/// <summary>Происходит при щелчке элемента управления мышью.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatAction"), SRDescription("ControlOnMouseClickDescr")]
		public event MouseEventHandler MouseClick
		{
			add
			{
				base.Events.AddHandler(Control.EventMouseClick, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventMouseClick, value);
			}
		}

		/// <summary>Генерируется при двойном щелчке элемента управления мышью.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatAction"), SRDescription("ControlOnMouseDoubleClickDescr")]
		public event MouseEventHandler MouseDoubleClick
		{
			add
			{
				base.Events.AddHandler(Control.EventMouseDoubleClick, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventMouseDoubleClick, value);
			}
		}

		/// <summary>Происходит, когда элемент управления теряет или получает захват мыши.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatAction"), SRDescription("ControlOnMouseCaptureChangedDescr")]
		public event EventHandler MouseCaptureChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventMouseCaptureChanged, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventMouseCaptureChanged, value);
			}
		}

		/// <summary>Происходит при нажатии кнопки мыши, если указатель мыши находится на элементе управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatMouse"), SRDescription("ControlOnMouseDownDescr")]
		public event MouseEventHandler MouseDown
		{
			add
			{
				base.Events.AddHandler(Control.EventMouseDown, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventMouseDown, value);
			}
		}

		/// <summary>Происходит, когда указатель мыши оказывается на элементе управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatMouse"), SRDescription("ControlOnMouseEnterDescr")]
		public event EventHandler MouseEnter
		{
			add
			{
				base.Events.AddHandler(Control.EventMouseEnter, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventMouseEnter, value);
			}
		}

		/// <summary>Происходит, когда указатель мыши покидает элемент управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatMouse"), SRDescription("ControlOnMouseLeaveDescr")]
		public event EventHandler MouseLeave
		{
			add
			{
				base.Events.AddHandler(Control.EventMouseLeave, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventMouseLeave, value);
			}
		}

		/// <summary>Происходит, когда указатель мыши задерживается на элементе управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatMouse"), SRDescription("ControlOnMouseHoverDescr")]
		public event EventHandler MouseHover
		{
			add
			{
				base.Events.AddHandler(Control.EventMouseHover, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventMouseHover, value);
			}
		}

		/// <summary>Происходит при перемещении указателя мыши по элементу управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatMouse"), SRDescription("ControlOnMouseMoveDescr")]
		public event MouseEventHandler MouseMove
		{
			add
			{
				base.Events.AddHandler(Control.EventMouseMove, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventMouseMove, value);
			}
		}

		/// <summary>Происходит при отпускании кнопки мыши, когда указатель мыши находится на элементе управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatMouse"), SRDescription("ControlOnMouseUpDescr")]
		public event MouseEventHandler MouseUp
		{
			add
			{
				base.Events.AddHandler(Control.EventMouseUp, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventMouseUp, value);
			}
		}

		/// <summary>Генерируется при движении колесика мыши, если элемент управления имеет фокус.</summary>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatMouse"), SRDescription("ControlOnMouseWheelDescr")]
		public event MouseEventHandler MouseWheel
		{
			add
			{
				base.Events.AddHandler(Control.EventMouseWheel, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventMouseWheel, value);
			}
		}

		/// <summary>Происходит при перемещении элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatLayout"), SRDescription("ControlOnMoveDescr")]
		public event EventHandler Move
		{
			add
			{
				base.Events.AddHandler(Control.EventMove, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventMove, value);
			}
		}

		/// <summary>Генерируется перед событием <see cref="E:System.Windows.Forms.Control.KeyDown" /> при нажатии клавиши, когда элемент управления имеет фокус.</summary>
		[SRCategory("CatKey"), SRDescription("PreviewKeyDownDescr")]
		public event PreviewKeyDownEventHandler PreviewKeyDown
		{
			[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			add
			{
				base.Events.AddHandler(Control.EventPreviewKeyDown, value);
			}
			[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			remove
			{
				base.Events.RemoveHandler(Control.EventPreviewKeyDown, value);
			}
		}

		/// <summary>Происходит при изменении размеров элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatLayout"), SRDescription("ControlOnResizeDescr")]
		public event EventHandler Resize
		{
			add
			{
				base.Events.AddHandler(Control.EventResize, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventResize, value);
			}
		}

		/// <summary>Происходит при изменении фокуса или клавиатурных подсказок пользовательского интерфейса.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatBehavior"), SRDescription("ControlOnChangeUICuesDescr")]
		public event UICuesEventHandler ChangeUICues
		{
			add
			{
				base.Events.AddHandler(Control.EventChangeUICues, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventChangeUICues, value);
			}
		}

		/// <summary>Происходит при изменении стиля элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatBehavior"), SRDescription("ControlOnStyleChangedDescr")]
		public event EventHandler StyleChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventStyleChanged, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventStyleChanged, value);
			}
		}

		/// <summary>Происходит при изменении системных цветов.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatBehavior"), SRDescription("ControlOnSystemColorsChangedDescr")]
		public event EventHandler SystemColorsChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventSystemColorsChanged, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventSystemColorsChanged, value);
			}
		}

		/// <summary>Происходит при проверке элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatFocus"), SRDescription("ControlOnValidatingDescr")]
		public event CancelEventHandler Validating
		{
			add
			{
				base.Events.AddHandler(Control.EventValidating, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventValidating, value);
			}
		}

		/// <summary>Происходит по завершении проверки элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatFocus"), SRDescription("ControlOnValidatedDescr")]
		public event EventHandler Validated
		{
			add
			{
				base.Events.AddHandler(Control.EventValidated, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventValidated, value);
			}
		}

		/// <summary>Происходит при изменении значения свойства <see cref="P:System.Windows.Forms.Control.Parent" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRCategory("CatPropertyChanged"), SRDescription("ControlOnParentChangedDescr")]
		public event EventHandler ParentChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventParent, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventParent, value);
			}
		}

		/// <summary>Происходит при изменении свойства <see cref="P:System.Windows.Forms.Control.ImeMode" />.</summary>
		/// <filterpriority>1</filterpriority>
		[SRDescription("ControlOnImeModeChangedDescr"), WinCategory("Behavior")]
		public event EventHandler ImeModeChanged
		{
			add
			{
				base.Events.AddHandler(Control.EventImeModeChanged, value);
			}
			remove
			{
				base.Events.RemoveHandler(Control.EventImeModeChanged, value);
			}
		}

		/// <summary>Получает объекты <see cref="T:System.Windows.Forms.AccessibleObject" />, назначенные элементу управления.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.AccessibleObject" />, назначенный элементу управления.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlAccessibilityObjectDescr")]
		public AccessibleObject AccessibilityObject
		{
			get
			{
				AccessibleObject accessibleObject = (AccessibleObject)this.Properties.GetObject(Control.PropAccessibility);
				if (accessibleObject == null)
				{
					accessibleObject = this.CreateAccessibilityInstance();
					if (!(accessibleObject is Control.ControlAccessibleObject))
					{
						return null;
					}
					this.Properties.SetObject(Control.PropAccessibility, accessibleObject);
				}
				return accessibleObject;
			}
		}

		private AccessibleObject NcAccessibilityObject
		{
			get
			{
				AccessibleObject accessibleObject = (AccessibleObject)this.Properties.GetObject(Control.PropNcAccessibility);
				if (accessibleObject == null)
				{
					accessibleObject = new Control.ControlAccessibleObject(this, 0);
					this.Properties.SetObject(Control.PropNcAccessibility, accessibleObject);
				}
				return accessibleObject;
			}
		}

		/// <summary>Возвращает или задает описание выполняемого по умолчанию действия элемента управления для использования клиентскими приложениями со специальными возможностями.</summary>
		/// <returns>Выполняемое по умолчанию действие элемента управления для использования клиентскими приложениями со специальными возможностями.</returns>
		/// <filterpriority>2</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatAccessibility"), SRDescription("ControlAccessibleDefaultActionDescr")]
		public string AccessibleDefaultActionDescription
		{
			get
			{
				return (string)this.Properties.GetObject(Control.PropAccessibleDefaultActionDescription);
			}
			set
			{
				this.Properties.SetObject(Control.PropAccessibleDefaultActionDescription, value);
			}
		}

		/// <summary>Возвращает или задает описание элемента управления, используемого клиентскими приложениями со специальными возможностями.</summary>
		/// <returns>Описание элемента управления, используемого клиентскими приложениями со специальными возможностями.Значение по умолчанию — null.</returns>
		/// <filterpriority>1</filterpriority>
		[DefaultValue(null), Localizable(true), SRCategory("CatAccessibility"), SRDescription("ControlAccessibleDescriptionDescr")]
		public string AccessibleDescription
		{
			get
			{
				return (string)this.Properties.GetObject(Control.PropAccessibleDescription);
			}
			set
			{
				this.Properties.SetObject(Control.PropAccessibleDescription, value);
			}
		}

		/// <summary>Возвращает или задает имя элемента управления, используемого клиентскими приложениями со специальными возможностями.</summary>
		/// <returns>Имя элемента управления, используемого клиентскими приложениями со специальными возможностями.Значением по умолчанию является null.</returns>
		/// <filterpriority>2</filterpriority>
		[DefaultValue(null), Localizable(true), SRCategory("CatAccessibility"), SRDescription("ControlAccessibleNameDescr")]
		public string AccessibleName
		{
			get
			{
				return (string)this.Properties.GetObject(Control.PropAccessibleName);
			}
			set
			{
				this.Properties.SetObject(Control.PropAccessibleName, value);
			}
		}

		/// <summary>Возвращает или задает роль элемента управления в поддержке специальных возможностей. </summary>
		/// <returns>Одно из значений <see cref="T:System.Windows.Forms.AccessibleRole" />.Значением по умолчанию является Default.</returns>
		/// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">Назначенное значение не является одним из значений <see cref="T:System.Windows.Forms.AccessibleRole" />. </exception>
		/// <filterpriority>2</filterpriority>
		[DefaultValue(AccessibleRole.Default), SRCategory("CatAccessibility"), SRDescription("ControlAccessibleRoleDescr")]
		public AccessibleRole AccessibleRole
		{
			get
			{
				bool flag;
				int integer = this.Properties.GetInteger(Control.PropAccessibleRole, out flag);
				if (flag)
				{
					return (AccessibleRole)integer;
				}
				return AccessibleRole.Default;
			}
			set
			{
				if (!ClientUtils.IsEnumValid(value, (int)value, -1, 64))
				{
					throw new InvalidEnumArgumentException("value", (int)value, typeof(AccessibleRole));
				}
				this.Properties.SetInteger(Control.PropAccessibleRole, (int)value);
			}
		}

		private Color ActiveXAmbientBackColor
		{
			get
			{
				return this.ActiveXInstance.AmbientBackColor;
			}
		}

		private Color ActiveXAmbientForeColor
		{
			get
			{
				return this.ActiveXInstance.AmbientForeColor;
			}
		}

		private Font ActiveXAmbientFont
		{
			get
			{
				return this.ActiveXInstance.AmbientFont;
			}
		}

		private bool ActiveXEventsFrozen
		{
			get
			{
				return this.ActiveXInstance.EventsFrozen;
			}
		}

		private IntPtr ActiveXHWNDParent
		{
			get
			{
				return this.ActiveXInstance.HWNDParent;
			}
		}

		private Control.ActiveXImpl ActiveXInstance
		{
			get
			{
				Control.ActiveXImpl activeXImpl = (Control.ActiveXImpl)this.Properties.GetObject(Control.PropActiveXImpl);
				if (activeXImpl == null)
				{
					if (this.GetState(524288))
					{
						throw new NotSupportedException(SR.GetString("AXTopLevelSource"));
					}
					activeXImpl = new Control.ActiveXImpl(this);
					this.SetState2(1024, true);
					this.Properties.SetObject(Control.PropActiveXImpl, activeXImpl);
				}
				return activeXImpl;
			}
		}

		/// <summary>Возвращает или задает значение, указывающее, может ли элемент управления принимать данные, перетаскиваемые в него пользователем.</summary>
		/// <returns>Значение true, если операции перетаскивания поддерживаются элементом управления; в противном случае — значение false.Значением по умолчанию является false.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[DefaultValue(false), SRCategory("CatBehavior"), SRDescription("ControlAllowDropDescr")]
		public virtual bool AllowDrop
		{
			get
			{
				return this.GetState(64);
			}
			set
			{
				if (this.GetState(64) != value)
				{
					if (value && !this.IsHandleCreated)
					{
						IntSecurity.ClipboardRead.Demand();
					}
					this.SetState(64, value);
					if (this.IsHandleCreated)
					{
						try
						{
							this.SetAcceptDrops(value);
						}
						catch
						{
							this.SetState(64, !value);
							throw;
						}
					}
				}
			}
		}

		private AmbientProperties AmbientPropertiesService
		{
			get
			{
				bool flag;
				AmbientProperties ambientProperties = (AmbientProperties)this.Properties.GetObject(Control.PropAmbientPropertiesService, out flag);
				if (!flag)
				{
					if (this.Site != null)
					{
						ambientProperties = (AmbientProperties)this.Site.GetService(typeof(AmbientProperties));
					}
					else
					{
						ambientProperties = (AmbientProperties)this.GetService(typeof(AmbientProperties));
					}
					if (ambientProperties != null)
					{
						this.Properties.SetObject(Control.PropAmbientPropertiesService, ambientProperties);
					}
				}
				return ambientProperties;
			}
		}

		/// <summary>Возвращает или задает границы контейнера, с которым связан элемент управления, и определяет способ изменения размеров элемента управления при изменении размеров его родительского элемента. </summary>
		/// <returns>Битовая комбинация значений <see cref="T:System.Windows.Forms.AnchorStyles" />.Значения по умолчанию — Top и Left.</returns>
		/// <filterpriority>1</filterpriority>
		[DefaultValue(AnchorStyles.Top | AnchorStyles.Left), Localizable(true), RefreshProperties(RefreshProperties.Repaint), SRCategory("CatLayout"), SRDescription("ControlAnchorDescr")]
		public virtual AnchorStyles Anchor
		{
			get
			{
				return DefaultLayout.GetAnchor(this);
			}
			set
			{
				DefaultLayout.SetAnchor(this.ParentInternal, this, value);
			}
		}

		/// <summary>Данное свойство не относится к этому классу.</summary>
		/// <returns>Значение true, если включено, в противном случае — значение false.</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), DefaultValue(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), Localizable(true), RefreshProperties(RefreshProperties.All), SRCategory("CatLayout"), SRDescription("ControlAutoSizeDescr")]
		public virtual bool AutoSize
		{
			get
			{
				return CommonProperties.GetAutoSize(this);
			}
			set
			{
				if (value != this.AutoSize)
				{
					CommonProperties.SetAutoSize(this, value);
					if (this.ParentInternal != null)
					{
						if (value && this.ParentInternal.LayoutEngine == DefaultLayout.Instance)
						{
							this.ParentInternal.LayoutEngine.InitLayout(this, BoundsSpecified.Size);
						}
						LayoutTransaction.DoLayout(this.ParentInternal, this, PropertyNames.AutoSize);
					}
					this.OnAutoSizeChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>Возвращает или задает местоположение, в котором выполняется прокрутка этого элемента управления в <see cref="M:System.Windows.Forms.ScrollableControl.ScrollControlIntoView(System.Windows.Forms.Control)" />.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Point" />, задающий местоположение выполнения прокрутки.По умолчанию это левый верхний угол элемента управления.</returns>
		[Browsable(false), DefaultValue(typeof(Point), "0, 0"), EditorBrowsable(EditorBrowsableState.Advanced)]
		public virtual Point AutoScrollOffset
		{
			get
			{
				if (this.Properties.ContainsObject(Control.PropAutoScrollOffset))
				{
					return (Point)this.Properties.GetObject(Control.PropAutoScrollOffset);
				}
				return Point.Empty;
			}
			set
			{
				if (this.AutoScrollOffset != value)
				{
					this.Properties.SetObject(Control.PropAutoScrollOffset, value);
				}
			}
		}

		/// <summary>Получает кэшированный экземпляр обработчика макета элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.Layout.LayoutEngine" /> для содержимого элемента управления.</returns>
		/// <filterpriority>2</filterpriority>
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Advanced)]
		public virtual LayoutEngine LayoutEngine
		{
			get
			{
				return DefaultLayout.Instance;
			}
		}

		internal IntPtr BackColorBrush
		{
			get
			{
				object @object = this.Properties.GetObject(Control.PropBackBrush);
				if (@object != null)
				{
					return (IntPtr)@object;
				}
				if (!this.Properties.ContainsObject(Control.PropBackColor) && this.parent != null && this.parent.BackColor == this.BackColor)
				{
					return this.parent.BackColorBrush;
				}
				Color backColor = this.BackColor;
				IntPtr intPtr;
				if (ColorTranslator.ToOle(backColor) < 0)
				{
					intPtr = SafeNativeMethods.GetSysColorBrush(ColorTranslator.ToOle(backColor) & 255);
					this.SetState(2097152, false);
				}
				else
				{
					intPtr = SafeNativeMethods.CreateSolidBrush(ColorTranslator.ToWin32(backColor));
					this.SetState(2097152, true);
				}
				this.Properties.SetObject(Control.PropBackBrush, intPtr);
				return intPtr;
			}
		}

		/// <summary>Возвращает или задает цвет фона для элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Color" />, представляющий собой цвет фона элемента управления.Значением по умолчанию является значение свойства <see cref="P:System.Windows.Forms.Control.DefaultBackColor" />.</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[DispId(-501), SRCategory("CatAppearance"), SRDescription("ControlBackColorDescr")]
		public virtual Color BackColor
		{
			get
			{
				Color color = this.RawBackColor;
				if (!color.IsEmpty)
				{
					return color;
				}
				Control parentInternal = this.ParentInternal;
				if (parentInternal != null && parentInternal.CanAccessProperties)
				{
					color = parentInternal.BackColor;
					if (this.IsValidBackColor(color))
					{
						return color;
					}
				}
				if (this.IsActiveX)
				{
					color = this.ActiveXAmbientBackColor;
				}
				if (color.IsEmpty)
				{
					AmbientProperties ambientPropertiesService = this.AmbientPropertiesService;
					if (ambientPropertiesService != null)
					{
						color = ambientPropertiesService.BackColor;
					}
				}
				if (!color.IsEmpty && this.IsValidBackColor(color))
				{
					return color;
				}
				return Control.DefaultBackColor;
			}
			set
			{
				if (!value.Equals(Color.Empty) && !this.GetStyle(ControlStyles.SupportsTransparentBackColor) && value.A < 255)
				{
					throw new ArgumentException(SR.GetString("TransparentBackColorNotAllowed"));
				}
				Color backColor = this.BackColor;
				if (!value.IsEmpty || this.Properties.ContainsObject(Control.PropBackColor))
				{
					this.Properties.SetColor(Control.PropBackColor, value);
				}
				if (!backColor.Equals(this.BackColor))
				{
					this.OnBackColorChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>Возвращает или задает фоновое изображение, выводимое на элементе управления.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Image" />, предоставляющий рисунок, который отображается в качестве фона элемента управления.</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[DefaultValue(null), Localizable(true), SRCategory("CatAppearance"), SRDescription("ControlBackgroundImageDescr")]
		public virtual Image BackgroundImage
		{
			get
			{
				return (Image)this.Properties.GetObject(Control.PropBackgroundImage);
			}
			set
			{
				if (this.BackgroundImage != value)
				{
					this.Properties.SetObject(Control.PropBackgroundImage, value);
					this.OnBackgroundImageChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>Возвращает или задает макет фонового изображения в соответствии с его определением в перечислении <see cref="T:System.Windows.Forms.ImageLayout" />.</summary>
		/// <returns>Одно из значений <see cref="T:System.Windows.Forms.ImageLayout" /> (<see cref="F:System.Windows.Forms.ImageLayout.Center" />, <see cref="F:System.Windows.Forms.ImageLayout.None" />, <see cref="F:System.Windows.Forms.ImageLayout.Stretch" />, <see cref="F:System.Windows.Forms.ImageLayout.Tile" /> или <see cref="F:System.Windows.Forms.ImageLayout.Zoom" />).<see cref="F:System.Windows.Forms.ImageLayout.Tile" /> является значением по умолчанию.</returns>
		/// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">Указанное значение перечисления не существует. </exception>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[DefaultValue(ImageLayout.Tile), Localizable(true), SRCategory("CatAppearance"), SRDescription("ControlBackgroundImageLayoutDescr")]
		public virtual ImageLayout BackgroundImageLayout
		{
			get
			{
				if (!this.Properties.ContainsObject(Control.PropBackgroundImageLayout))
				{
					return ImageLayout.Tile;
				}
				return (ImageLayout)this.Properties.GetObject(Control.PropBackgroundImageLayout);
			}
			set
			{
				if (this.BackgroundImageLayout != value)
				{
					if (!ClientUtils.IsEnumValid(value, (int)value, 0, 4))
					{
						throw new InvalidEnumArgumentException("value", (int)value, typeof(ImageLayout));
					}
					if (value == ImageLayout.Center || value == ImageLayout.Zoom || value == ImageLayout.Stretch)
					{
						this.SetStyle(ControlStyles.ResizeRedraw, true);
						if (ControlPaint.IsImageTransparent(this.BackgroundImage))
						{
							this.DoubleBuffered = true;
						}
					}
					this.Properties.SetObject(Control.PropBackgroundImageLayout, value);
					this.OnBackgroundImageLayoutChanged(EventArgs.Empty);
				}
			}
		}

		internal bool BecomingActiveControl
		{
			get
			{
				return this.GetState2(32);
			}
			set
			{
				if (value != this.BecomingActiveControl)
				{
					Application.ThreadContext.FromCurrent().ActivatingControl = (value ? this : null);
					this.SetState2(32, value);
				}
			}
		}

		internal BindingContext BindingContextInternal
		{
			get
			{
				BindingContext bindingContext = (BindingContext)this.Properties.GetObject(Control.PropBindingManager);
				if (bindingContext != null)
				{
					return bindingContext;
				}
				Control parentInternal = this.ParentInternal;
				if (parentInternal != null && parentInternal.CanAccessProperties)
				{
					return parentInternal.BindingContext;
				}
				return null;
			}
			set
			{
				if ((BindingContext)this.Properties.GetObject(Control.PropBindingManager) != value)
				{
					this.Properties.SetObject(Control.PropBindingManager, value);
					this.OnBindingContextChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>Возвращает или задает объект <see cref="T:System.Windows.Forms.BindingContext" /> для элемента управления.</summary>
		/// <returns>Свойство <see cref="T:System.Windows.Forms.BindingContext" /> элемента управления.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlBindingContextDescr")]
		public virtual BindingContext BindingContext
		{
			get
			{
				return this.BindingContextInternal;
			}
			set
			{
				this.BindingContextInternal = value;
			}
		}

		/// <summary>Получает расстояние (в точках) между нижней границей элемента управления и верхней границей клиентской области контейнера.</summary>
		/// <returns>Объект <see cref="T:System.Int32" />, представляющий расстояние (в точках) между нижней границей элемента управления и верхней границей клиентской области контейнера.</returns>
		/// <filterpriority>2</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatLayout"), SRDescription("ControlBottomDescr")]
		public int Bottom
		{
			get
			{
				return this.y + this.height;
			}
		}

		/// <summary>Возвращает или задает размер и местоположение (в точках) элемента управления, включая его неклиентские элементы, относительно его родительского элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Rectangle" /> в точках относительно родительского элемента управления, представляющий размер и местоположение элемента управления, включая его неклиентские элементы.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatLayout"), SRDescription("ControlBoundsDescr")]
		public Rectangle Bounds
		{
			get
			{
				return new Rectangle(this.x, this.y, this.width, this.height);
			}
			set
			{
				this.SetBounds(value.X, value.Y, value.Width, value.Height, BoundsSpecified.All);
			}
		}

		internal virtual bool CanAccessProperties
		{
			get
			{
				return true;
			}
		}

		/// <summary>Получает значение, показывающее, может ли элемент управления получать фокус.</summary>
		/// <returns>Значение true, если элемент управления может получать фокус; в противном случае — значение false.</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatFocus"), SRDescription("ControlCanFocusDescr")]
		public bool CanFocus
		{
			get
			{
				if (!this.IsHandleCreated)
				{
					return false;
				}
				bool arg_38_0 = SafeNativeMethods.IsWindowVisible(new HandleRef(this.window, this.Handle));
				bool flag = SafeNativeMethods.IsWindowEnabled(new HandleRef(this.window, this.Handle));
				return arg_38_0 & flag;
			}
		}

		/// <summary>Определяет, могут ли вызываться события в элементе управления.</summary>
		/// <returns>Значение true, если элемент управления размещен как управляющий элемент ActiveX, события которого заморожены; в противном случае — значение false.</returns>
		protected override bool CanRaiseEvents
		{
			get
			{
				return !this.IsActiveX || !this.ActiveXEventsFrozen;
			}
		}

		/// <summary>Получает значение, показывающее, доступен ли элемент управления для выбора.</summary>
		/// <returns>Значение true, если элемент управления может быть выбран; в противном случае — значение false.</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatFocus"), SRDescription("ControlCanSelectDescr")]
		public bool CanSelect
		{
			get
			{
				return this.CanSelectCore();
			}
		}

		/// <summary>Возвращает или задает значение, определяющее, была ли мышь захвачена элементом управления.</summary>
		/// <returns>Значение true, если мышь захвачена элементом управления; в противном случае — значение false.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatFocus"), SRDescription("ControlCaptureDescr")]
		public bool Capture
		{
			get
			{
				return this.CaptureInternal;
			}
			set
			{
				if (value)
				{
					IntSecurity.GetCapture.Demand();
				}
				this.CaptureInternal = value;
			}
		}

		internal bool CaptureInternal
		{
			get
			{
				return this.IsHandleCreated && UnsafeNativeMethods.GetCapture() == this.Handle;
			}
			set
			{
				if (this.CaptureInternal != value)
				{
					if (value)
					{
						UnsafeNativeMethods.SetCapture(new HandleRef(this, this.Handle));
						return;
					}
					SafeNativeMethods.ReleaseCapture();
				}
			}
		}

		/// <summary>Возвращает или задает значение, показывающее, вызывает ли элемент управления выполнение проверки.</summary>
		/// <returns>Значение true, если элемент управления вызывает выполнение проверки в любом элементе управления, требующем проверки при получении фокуса; в противном случае — значение false.Значением по умолчанию является true.</returns>
		/// <filterpriority>2</filterpriority>
		[DefaultValue(true), SRCategory("CatFocus"), SRDescription("ControlCausesValidationDescr")]
		public bool CausesValidation
		{
			get
			{
				return this.GetState(131072);
			}
			set
			{
				if (value != this.CausesValidation)
				{
					this.SetState(131072, value);
					this.OnCausesValidationChanged(EventArgs.Empty);
				}
			}
		}

		internal bool CacheTextInternal
		{
			get
			{
				bool flag;
				return this.Properties.GetInteger(Control.PropCacheTextCount, out flag) > 0 || this.GetStyle(ControlStyles.CacheText);
			}
			set
			{
				if (this.GetStyle(ControlStyles.CacheText) || !this.IsHandleCreated)
				{
					return;
				}
				bool flag;
				int num = this.Properties.GetInteger(Control.PropCacheTextCount, out flag);
				if (value)
				{
					if (num == 0)
					{
						this.Properties.SetObject(Control.PropCacheTextField, this.text);
						if (this.text == null)
						{
							this.text = this.WindowText;
						}
					}
					num++;
				}
				else
				{
					num--;
					if (num == 0)
					{
						this.text = (string)this.Properties.GetObject(Control.PropCacheTextField, out flag);
					}
				}
				this.Properties.SetInteger(Control.PropCacheTextCount, num);
			}
		}

		/// <summary>Возвращает или задает значение, показывающее, нужно ли перехватывать вызовы в ошибочном потоке, который осуществляет доступ к свойству <see cref="P:System.Windows.Forms.Control.Handle" /> элемента управления во время отладки.</summary>
		/// <returns>Значение true, если вызовы в ошибочном потоке перехватываются; в противном случае — значение false.</returns>
		/// <filterpriority>2</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlCheckForIllegalCrossThreadCalls")]
		public static bool CheckForIllegalCrossThreadCalls
		{
			get
			{
				return Control.checkForIllegalCrossThreadCalls;
			}
			set
			{
				Control.checkForIllegalCrossThreadCalls = value;
			}
		}

		/// <summary>Получает прямоугольник, представляющий клиентскую область элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Rectangle" />, представляющий клиентскую область элемента управления.</returns>
		/// <filterpriority>2</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatLayout"), SRDescription("ControlClientRectangleDescr")]
		public Rectangle ClientRectangle
		{
			get
			{
				return new Rectangle(0, 0, this.clientWidth, this.clientHeight);
			}
		}

		/// <summary>Возвращает или задает высоту и ширину клиентской области элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Size" />, представляющий измерения клиентской области элемента управления.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatLayout"), SRDescription("ControlClientSizeDescr")]
		public Size ClientSize
		{
			get
			{
				return new Size(this.clientWidth, this.clientHeight);
			}
			set
			{
				this.SetClientSizeCore(value.Width, value.Height);
			}
		}

		/// <summary>Получает название организации или имя создателя приложения, содержащего элемент управления.</summary>
		/// <returns>Название организации или имя создателя приложения, содержащего элемент управления.</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), Description("ControlCompanyNameDescr"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced)]
		public string CompanyName
		{
			get
			{
				return this.VersionInfo.CompanyName;
			}
		}

		/// <summary>Получает значение, указывающее, имеет ли элемент управления или один из его дочерних элементов фокус ввода в данный момент.</summary>
		/// <returns>Значение true, если элемент управления или один из его дочерних элементов в данный момент имеет фокус ввода; в противном случае — значение false.</returns>
		/// <filterpriority>2</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlContainsFocusDescr")]
		public bool ContainsFocus
		{
			get
			{
				if (!this.IsHandleCreated)
				{
					return false;
				}
				IntPtr focus = UnsafeNativeMethods.GetFocus();
				return !(focus == IntPtr.Zero) && (focus == this.Handle || UnsafeNativeMethods.IsChild(new HandleRef(this, this.Handle), new HandleRef(this, focus)));
			}
		}

		/// <summary>Возвращает или задает контекстное меню, сопоставленное с элементом управления.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.ContextMenu" /> предоставляет контекстное меню, сопоставленное с элементом управления.</returns>
		/// <filterpriority>2</filterpriority>
		[Browsable(false), DefaultValue(null), SRCategory("CatBehavior"), SRDescription("ControlContextMenuDescr")]
		public virtual ContextMenu ContextMenu
		{
			get
			{
				return (ContextMenu)this.Properties.GetObject(Control.PropContextMenu);
			}
			set
			{
				ContextMenu contextMenu = (ContextMenu)this.Properties.GetObject(Control.PropContextMenu);
				if (contextMenu != value)
				{
					EventHandler value2 = new EventHandler(this.DetachContextMenu);
					if (contextMenu != null)
					{
						contextMenu.Disposed -= value2;
					}
					this.Properties.SetObject(Control.PropContextMenu, value);
					if (value != null)
					{
						value.Disposed += value2;
					}
					this.OnContextMenuChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>Возвращает или задает объект <see cref="T:System.Windows.Forms.ContextMenuStrip" />, сопоставленный с этим элементом управления.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.ContextMenuStrip" /> для этого элемента управления или значение null, если объект <see cref="T:System.Windows.Forms.ContextMenuStrip" /> отсутствует.Значение по умолчанию — null.</returns>
		[DefaultValue(null), SRCategory("CatBehavior"), SRDescription("ControlContextMenuDescr")]
		public virtual ContextMenuStrip ContextMenuStrip
		{
			get
			{
				return (ContextMenuStrip)this.Properties.GetObject(Control.PropContextMenuStrip);
			}
			set
			{
				ContextMenuStrip contextMenuStrip = this.Properties.GetObject(Control.PropContextMenuStrip) as ContextMenuStrip;
				if (contextMenuStrip != value)
				{
					EventHandler value2 = new EventHandler(this.DetachContextMenuStrip);
					if (contextMenuStrip != null)
					{
						contextMenuStrip.Disposed -= value2;
					}
					this.Properties.SetObject(Control.PropContextMenuStrip, value);
					if (value != null)
					{
						value.Disposed += value2;
					}
					this.OnContextMenuStripChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>Получает коллекцию элементов управления, содержащихся в элементе управления.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.Control.ControlCollection" /> представляет коллекцию элементов управления, содержащихся в элементе управления.</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Content), SRDescription("ControlControlsDescr")]
		public Control.ControlCollection Controls
		{
			get
			{
				Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
				if (controlCollection == null)
				{
					controlCollection = this.CreateControlsInstance();
					this.Properties.SetObject(Control.PropControlsCollection, controlCollection);
				}
				return controlCollection;
			}
		}

		/// <summary>Получает значение, показывающее, был ли создан элемент управления.</summary>
		/// <returns>Значение true, если элемент был создан; в противном случае — значение false.</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlCreatedDescr")]
		public bool Created
		{
			get
			{
				return (this.state & 1) != 0;
			}
		}

		/// <summary>Получает параметры, необходимые для создания дескриптора элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.CreateParams" />, содержащий необходимые параметры процедуры создания дескриптора элемента управления.</returns>
		protected virtual CreateParams CreateParams
		{
			[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode), SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			get
			{
				if (UnsafeNativeMethods.GetModuleHandle("comctl32.dll") == IntPtr.Zero && UnsafeNativeMethods.LoadLibrary("comctl32.dll") == IntPtr.Zero)
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), SR.GetString("LoadDLLError", new object[]
					{
						"comctl32.dll"
					}));
				}
				if (this.createParams == null)
				{
					this.createParams = new CreateParams();
				}
				CreateParams createParams = this.createParams;
				createParams.Style = 0;
				createParams.ExStyle = 0;
				createParams.ClassStyle = 0;
				createParams.Caption = this.text;
				createParams.X = this.x;
				createParams.Y = this.y;
				createParams.Width = this.width;
				createParams.Height = this.height;
				createParams.Style = 33554432;
				if (this.GetStyle(ControlStyles.ContainerControl))
				{
					createParams.ExStyle |= 65536;
				}
				createParams.ClassStyle = 8;
				if ((this.state & 524288) == 0)
				{
					createParams.Parent = ((this.parent == null) ? IntPtr.Zero : this.parent.InternalHandle);
					createParams.Style |= 1140850688;
				}
				else
				{
					createParams.Parent = IntPtr.Zero;
				}
				if ((this.state & 8) != 0)
				{
					createParams.Style |= 65536;
				}
				if ((this.state & 2) != 0)
				{
					createParams.Style |= 268435456;
				}
				if (!this.Enabled)
				{
					createParams.Style |= 134217728;
				}
				if (createParams.Parent == IntPtr.Zero && this.IsActiveX)
				{
					createParams.Parent = this.ActiveXHWNDParent;
				}
				if (this.RightToLeft == RightToLeft.Yes)
				{
					createParams.ExStyle |= 8192;
					createParams.ExStyle |= 4096;
					createParams.ExStyle |= 16384;
				}
				return createParams;
			}
		}

		internal bool ValidationCancelled
		{
			get
			{
				if (this.GetState(268435456))
				{
					return true;
				}
				Control parentInternal = this.ParentInternal;
				return parentInternal != null && parentInternal.ValidationCancelled;
			}
			set
			{
				this.SetState(268435456, value);
			}
		}

		internal bool IsTopMdiWindowClosing
		{
			get
			{
				return this.GetState2(4096);
			}
			set
			{
				this.SetState2(4096, value);
			}
		}

		internal int CreateThreadId
		{
			get
			{
				if (this.IsHandleCreated)
				{
					int num;
					return SafeNativeMethods.GetWindowThreadProcessId(new HandleRef(this, this.Handle), out num);
				}
				return SafeNativeMethods.GetCurrentThreadId();
			}
		}

		/// <summary>Возвращает или задает курсор, отображаемый, когда указатель мыши находится на элементе управления.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.Cursor" />, который представляет курсор, отображаемый, когда указатель мыши находится на элементе управления.</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[AmbientValue(null), SRCategory("CatAppearance"), SRDescription("ControlCursorDescr")]
		public virtual Cursor Cursor
		{
			get
			{
				if (this.GetState(1024))
				{
					return Cursors.WaitCursor;
				}
				Cursor cursor = (Cursor)this.Properties.GetObject(Control.PropCursor);
				if (cursor != null)
				{
					return cursor;
				}
				Cursor defaultCursor = this.DefaultCursor;
				if (defaultCursor != Cursors.Default)
				{
					return defaultCursor;
				}
				Control parentInternal = this.ParentInternal;
				if (parentInternal != null)
				{
					return parentInternal.Cursor;
				}
				AmbientProperties ambientPropertiesService = this.AmbientPropertiesService;
				if (ambientPropertiesService != null && ambientPropertiesService.Cursor != null)
				{
					return ambientPropertiesService.Cursor;
				}
				return defaultCursor;
			}
			set
			{
				Cursor arg_1D_0 = (Cursor)this.Properties.GetObject(Control.PropCursor);
				Cursor cursor = this.Cursor;
				if (arg_1D_0 != value)
				{
					IntSecurity.ModifyCursor.Demand();
					this.Properties.SetObject(Control.PropCursor, value);
				}
				if (this.IsHandleCreated)
				{
					NativeMethods.POINT pOINT = new NativeMethods.POINT();
					NativeMethods.RECT rECT = default(NativeMethods.RECT);
					UnsafeNativeMethods.GetCursorPos(pOINT);
					UnsafeNativeMethods.GetWindowRect(new HandleRef(this, this.Handle), ref rECT);
					if ((rECT.left <= pOINT.x && pOINT.x < rECT.right && rECT.top <= pOINT.y && pOINT.y < rECT.bottom) || UnsafeNativeMethods.GetCapture() == this.Handle)
					{
						this.SendMessage(32, this.Handle, (IntPtr)1);
					}
				}
				if (!cursor.Equals(value))
				{
					this.OnCursorChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>Получает привязки данных для этого элемента управления.</summary>
		/// <returns>Коллекция <see cref="T:System.Windows.Forms.ControlBindingsCollection" />, содержащая объекты <see cref="T:System.Windows.Forms.Binding" /> для элемента управления. </returns>
		/// <filterpriority>1</filterpriority>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content), ParenthesizePropertyName(true), RefreshProperties(RefreshProperties.All), SRCategory("CatData"), SRDescription("ControlBindingsDescr")]
		public ControlBindingsCollection DataBindings
		{
			get
			{
				ControlBindingsCollection controlBindingsCollection = (ControlBindingsCollection)this.Properties.GetObject(Control.PropBindings);
				if (controlBindingsCollection == null)
				{
					controlBindingsCollection = new ControlBindingsCollection(this);
					this.Properties.SetObject(Control.PropBindings, controlBindingsCollection);
				}
				return controlBindingsCollection;
			}
		}

		/// <summary>Получает используемый по умолчанию цвет фона элемента управления.</summary>
		/// <returns>Используемый по умолчанию цвет <see cref="T:System.Drawing.Color" /> фона элемента управления.Значением по умолчанию является <see cref="P:System.Drawing.SystemColors.Control" />.</returns>
		/// <filterpriority>1</filterpriority>
		public static Color DefaultBackColor
		{
			get
			{
				return SystemColors.Control;
			}
		}

		/// <summary>Получает или задает курсор по умолчанию для элемента управления.</summary>
		/// <returns>Объект типа <see cref="T:System.Windows.Forms.Cursor" />, представляющий текущий курсор по умолчанию.</returns>
		protected virtual Cursor DefaultCursor
		{
			get
			{
				return Cursors.Default;
			}
		}

		/// <summary>Получает шрифт элемента управления, используемый по умолчанию.</summary>
		/// <returns>
		///   <see cref="T:System.Drawing.Font" /> элемента управления по умолчанию.Возвращаемое значение изменяется в зависимости от операционной системы пользователя, а также от языка и региональных параметров используемой системы.</returns>
		/// <exception cref="T:System.ArgumentException">На клиентском компьютере не установлен шрифт по умолчанию или другие шрифты, определяемые языком и региональными параметрами. </exception>
		/// <filterpriority>1</filterpriority>
		public static Font DefaultFont
		{
			get
			{
				if (Control.defaultFont == null)
				{
					Control.defaultFont = SystemFonts.DefaultFont;
				}
				return Control.defaultFont;
			}
		}

		/// <summary>Получает основной цвет элемента управления, используемый по умолчанию.</summary>
		/// <returns>Основной цвет <see cref="T:System.Drawing.Color" /> элемента управления, используемый по умолчанию.Значением по умолчанию является <see cref="P:System.Drawing.SystemColors.ControlText" />.</returns>
		/// <filterpriority>1</filterpriority>
		public static Color DefaultForeColor
		{
			get
			{
				return SystemColors.ControlText;
			}
		}

		/// <summary>Получает размер пустого пространства (в пикселях), по умолчанию оставляемого между элементами управления.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.Padding" />, который представляет заданное по умолчанию пустое пространство между элементами управления.</returns>
		protected virtual Padding DefaultMargin
		{
			get
			{
				return CommonProperties.DefaultMargin;
			}
		}

		/// <summary>Получает длину и высоту (в точках), которые были указаны в качестве максимального размера элемента управления.</summary>
		/// <returns>Метод <see cref="M:System.Drawing.Point.#ctor(System.Drawing.Size)" />, представляющий размер элемента управления.</returns>
		protected virtual Size DefaultMaximumSize
		{
			get
			{
				return CommonProperties.DefaultMaximumSize;
			}
		}

		/// <summary>Получает длину и высоту (в точках), которые были указаны в качестве минимального размера элемента управления.</summary>
		/// <returns>Метод <see cref="T:System.Drawing.Size" />, представляющий размер элемента управления.</returns>
		protected virtual Size DefaultMinimumSize
		{
			get
			{
				return CommonProperties.DefaultMinimumSize;
			}
		}

		/// <summary>Получает внутренние промежутки в содержимом элемента управления в точках.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.Padding" />, который представляет внутренние промежутки в содержимом элемента управления.</returns>
		protected virtual Padding DefaultPadding
		{
			get
			{
				return Padding.Empty;
			}
		}

		private RightToLeft DefaultRightToLeft
		{
			get
			{
				return RightToLeft.No;
			}
		}

		/// <summary>Получает размер элемента управления по умолчанию.</summary>
		/// <returns>
		///   <see cref="T:System.Drawing.Size" /> элемента управления по умолчанию.</returns>
		protected virtual Size DefaultSize
		{
			get
			{
				return Size.Empty;
			}
		}

		internal Color DisabledColor
		{
			get
			{
				Color result = this.BackColor;
				if (result.A == 0)
				{
					Control parentInternal = this.ParentInternal;
					while (result.A == 0)
					{
						if (parentInternal == null)
						{
							result = SystemColors.Control;
							break;
						}
						result = parentInternal.BackColor;
						parentInternal = parentInternal.ParentInternal;
					}
				}
				return result;
			}
		}

		/// <summary>Получает прямоугольник, представляющий отображаемую область элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Rectangle" />, представляющий отображаемую область элемента управления.</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlDisplayRectangleDescr")]
		public virtual Rectangle DisplayRectangle
		{
			get
			{
				return new Rectangle(0, 0, this.clientWidth, this.clientHeight);
			}
		}

		/// <summary>Получает значение, показывающее, был ли удален элемент управления.</summary>
		/// <returns>Значение true, если элемент был удален; в противном случае — значение false.</returns>
		/// <filterpriority>2</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlDisposedDescr")]
		public bool IsDisposed
		{
			get
			{
				return this.GetState(2048);
			}
		}

		/// <summary>Получает значение, указывающее, находится ли базовый класс <see cref="T:System.Windows.Forms.Control" /> в процессе удаления.</summary>
		/// <returns>Значение true, если базовый класс <see cref="T:System.Windows.Forms.Control" /> находится в процессе удаления; в противном случае — значение false.</returns>
		/// <filterpriority>2</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlDisposingDescr")]
		public bool Disposing
		{
			get
			{
				return this.GetState(4096);
			}
		}

		/// <summary>Возвращает или задает границы элемента управления, прикрепленные к его родительскому элементу управления, и определяет способ изменения размеров элемента управления с его родительским элементом управления.</summary>
		/// <returns>Одно из значений <see cref="T:System.Windows.Forms.DockStyle" />.Значением по умолчанию является <see cref="F:System.Windows.Forms.DockStyle.None" />.</returns>
		/// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">Назначенное значение не является одним из значений <see cref="T:System.Windows.Forms.DockStyle" />. </exception>
		/// <filterpriority>1</filterpriority>
		[DefaultValue(DockStyle.None), Localizable(true), RefreshProperties(RefreshProperties.Repaint), SRCategory("CatLayout"), SRDescription("ControlDockDescr")]
		public virtual DockStyle Dock
		{
			get
			{
				return DefaultLayout.GetDock(this);
			}
			set
			{
				if (value != this.Dock)
				{
					this.SuspendLayout();
					try
					{
						DefaultLayout.SetDock(this, value);
						this.OnDockChanged(EventArgs.Empty);
					}
					finally
					{
						this.ResumeLayout();
					}
				}
			}
		}

		/// <summary>Возвращает или задает значение, указывающее, должна ли поверхность этого элемента управления перерисовываться с помощью дополнительного буфера, чтобы уменьшить или предотвратить мерцание.</summary>
		/// <returns>Значение true, если поверхность элемента управления должна перерисовываться с помощью двойной буферизации; в противном случае — значение false.</returns>
		[SRCategory("CatBehavior"), SRDescription("ControlDoubleBufferedDescr")]
		protected virtual bool DoubleBuffered
		{
			get
			{
				return this.GetStyle(ControlStyles.OptimizedDoubleBuffer);
			}
			set
			{
				if (value != this.DoubleBuffered)
				{
					if (value)
					{
						this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value);
						return;
					}
					this.SetStyle(ControlStyles.OptimizedDoubleBuffer, value);
				}
			}
		}

		private bool DoubleBufferingEnabled
		{
			get
			{
				return this.GetStyle(ControlStyles.UserPaint | ControlStyles.DoubleBuffer);
			}
		}

		/// <summary>Возвращает или задает значение, показывающее, сможет ли элемент управления отвечать на действия пользователя.</summary>
		/// <returns>Значение true, если элемент управления может отвечать на действия пользователя; в противном случае — значение false.Значением по умолчанию является true.</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Localizable(true), DispId(-514), SRCategory("CatBehavior"), SRDescription("ControlEnabledDescr")]
		public bool Enabled
		{
			get
			{
				return this.GetState(4) && (this.ParentInternal == null || this.ParentInternal.Enabled);
			}
			set
			{
				bool arg_0F_0 = this.Enabled;
				this.SetState(4, value);
				if (arg_0F_0 != value)
				{
					if (!value)
					{
						this.SelectNextIfFocused();
					}
					this.OnEnabledChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>Получает значение, показывающее, имеется ли в элементе управления фокус ввода.</summary>
		/// <returns>Значение true, если элемент управления имеет фокус; в противном случае — значение false.</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlFocusedDescr")]
		public virtual bool Focused
		{
			get
			{
				return this.IsHandleCreated && UnsafeNativeMethods.GetFocus() == this.Handle;
			}
		}

		/// <summary>Возвращает или задает шрифт текста, отображаемого элементом управления.</summary>
		/// <returns>Шрифт <see cref="T:System.Drawing.Font" />, применяемый к тексту, отображаемому элементом управления.Значением по умолчанию является значение свойства <see cref="P:System.Windows.Forms.Control.DefaultFont" />.</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[AmbientValue(null), Localizable(true), DispId(-512), SRCategory("CatAppearance"), SRDescription("ControlFontDescr")]
		public virtual Font Font
		{
			[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "System.Windows.Forms.Control/ActiveXFontMarshaler")]
			get
			{
				Font font = (Font)this.Properties.GetObject(Control.PropFont);
				if (font != null)
				{
					return font;
				}
				Font font2 = this.GetParentFont();
				if (font2 != null)
				{
					return font2;
				}
				if (this.IsActiveX)
				{
					font2 = this.ActiveXAmbientFont;
					if (font2 != null)
					{
						return font2;
					}
				}
				AmbientProperties ambientPropertiesService = this.AmbientPropertiesService;
				if (ambientPropertiesService != null && ambientPropertiesService.Font != null)
				{
					return ambientPropertiesService.Font;
				}
				return Control.DefaultFont;
			}
			[param: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "System.Windows.Forms.Control/ActiveXFontMarshaler")]
			set
			{
				Font font = (Font)this.Properties.GetObject(Control.PropFont);
				Font font2 = this.Font;
				bool flag = false;
				if (value == null)
				{
					if (font != null)
					{
						flag = true;
					}
				}
				else
				{
					flag = (font == null || !value.Equals(font));
				}
				if (flag)
				{
					this.Properties.SetObject(Control.PropFont, value);
					if (!font2.Equals(value))
					{
						this.DisposeFontHandle();
						if (this.Properties.ContainsInteger(Control.PropFontHeight))
						{
							this.Properties.SetInteger(Control.PropFontHeight, (value == null) ? -1 : value.Height);
						}
						using (new LayoutTransaction(this.ParentInternal, this, PropertyNames.Font))
						{
							this.OnFontChanged(EventArgs.Empty);
							return;
						}
					}
					if (this.IsHandleCreated && !this.GetStyle(ControlStyles.UserPaint))
					{
						this.DisposeFontHandle();
						this.SetWindowFont();
					}
				}
			}
		}

		internal IntPtr FontHandle
		{
			get
			{
				Font font = (Font)this.Properties.GetObject(Control.PropFont);
				if (font != null)
				{
					Control.FontHandleWrapper fontHandleWrapper = (Control.FontHandleWrapper)this.Properties.GetObject(Control.PropFontHandleWrapper);
					if (fontHandleWrapper == null)
					{
						fontHandleWrapper = new Control.FontHandleWrapper(font);
						this.Properties.SetObject(Control.PropFontHandleWrapper, fontHandleWrapper);
					}
					return fontHandleWrapper.Handle;
				}
				if (this.parent != null)
				{
					return this.parent.FontHandle;
				}
				AmbientProperties ambientPropertiesService = this.AmbientPropertiesService;
				if (ambientPropertiesService != null && ambientPropertiesService.Font != null)
				{
					Control.FontHandleWrapper fontHandleWrapper2 = null;
					Font font2 = (Font)this.Properties.GetObject(Control.PropCurrentAmbientFont);
					if (font2 != null && font2 == ambientPropertiesService.Font)
					{
						fontHandleWrapper2 = (Control.FontHandleWrapper)this.Properties.GetObject(Control.PropFontHandleWrapper);
					}
					else
					{
						this.Properties.SetObject(Control.PropCurrentAmbientFont, ambientPropertiesService.Font);
					}
					if (fontHandleWrapper2 == null)
					{
						font = ambientPropertiesService.Font;
						fontHandleWrapper2 = new Control.FontHandleWrapper(font);
						this.Properties.SetObject(Control.PropFontHandleWrapper, fontHandleWrapper2);
					}
					return fontHandleWrapper2.Handle;
				}
				return Control.GetDefaultFontHandleWrapper().Handle;
			}
		}

		/// <summary>Возвращает или задает высоту шрифта элемента управления.</summary>
		/// <returns>Высота объекта <see cref="T:System.Drawing.Font" /> элемента управления (в точках).</returns>
		protected int FontHeight
		{
			get
			{
				bool flag;
				int integer = this.Properties.GetInteger(Control.PropFontHeight, out flag);
				if (flag && integer != -1)
				{
					return integer;
				}
				Font font = (Font)this.Properties.GetObject(Control.PropFont);
				if (font != null)
				{
					integer = font.Height;
					this.Properties.SetInteger(Control.PropFontHeight, integer);
					return integer;
				}
				int num = -1;
				if (this.ParentInternal != null && this.ParentInternal.CanAccessProperties)
				{
					num = this.ParentInternal.FontHeight;
				}
				if (num == -1)
				{
					num = this.Font.Height;
					this.Properties.SetInteger(Control.PropFontHeight, num);
				}
				return num;
			}
			set
			{
				this.Properties.SetInteger(Control.PropFontHeight, value);
			}
		}

		/// <summary>Получает или задает основной цвет элемента управления.</summary>
		/// <returns>Основной цвет <see cref="T:System.Drawing.Color" /> элемента управления.Значением по умолчанию является значение свойства <see cref="P:System.Windows.Forms.Control.DefaultForeColor" />.</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[DispId(-513), SRCategory("CatAppearance"), SRDescription("ControlForeColorDescr")]
		public virtual Color ForeColor
		{
			get
			{
				Color color = this.Properties.GetColor(Control.PropForeColor);
				if (!color.IsEmpty)
				{
					return color;
				}
				Control parentInternal = this.ParentInternal;
				if (parentInternal != null && parentInternal.CanAccessProperties)
				{
					return parentInternal.ForeColor;
				}
				Color result = Color.Empty;
				if (this.IsActiveX)
				{
					result = this.ActiveXAmbientForeColor;
				}
				if (result.IsEmpty)
				{
					AmbientProperties ambientPropertiesService = this.AmbientPropertiesService;
					if (ambientPropertiesService != null)
					{
						result = ambientPropertiesService.ForeColor;
					}
				}
				if (!result.IsEmpty)
				{
					return result;
				}
				return Control.DefaultForeColor;
			}
			set
			{
				Color foreColor = this.ForeColor;
				if (!value.IsEmpty || this.Properties.ContainsObject(Control.PropForeColor))
				{
					this.Properties.SetColor(Control.PropForeColor, value);
				}
				if (!foreColor.Equals(this.ForeColor))
				{
					this.OnForeColorChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>Получает дескриптор окна, с которым связан элемент управления.</summary>
		/// <returns>Объект <see cref="T:System.IntPtr" />, содержащий дескриптор окна (HWND) элемента управления.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), DispId(-515), SRDescription("ControlHandleDescr")]
		public IntPtr Handle
		{
			get
			{
				if (Control.checkForIllegalCrossThreadCalls && !Control.inCrossThreadSafeCall && this.InvokeRequired)
				{
					throw new InvalidOperationException(SR.GetString("IllegalCrossThreadCall", new object[]
					{
						this.Name
					}));
				}
				if (!this.IsHandleCreated)
				{
					this.CreateHandle();
				}
				return this.HandleInternal;
			}
		}

		internal IntPtr HandleInternal
		{
			get
			{
				return this.window.Handle;
			}
		}

		/// <summary>Получает значение, определяющее, содержит ли элемент управления один или несколько дочерних элементов.</summary>
		/// <returns>Значение true, если элемент управления содержит один или несколько элементов; в противном случае — значение false.</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlHasChildrenDescr")]
		public bool HasChildren
		{
			get
			{
				Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
				return controlCollection != null && controlCollection.Count > 0;
			}
		}

		internal virtual bool HasMenu
		{
			get
			{
				return false;
			}
		}

		/// <summary>Возвращает или задает высоту элемента управления.</summary>
		/// <returns>Высота элемента управления (в точках).</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Always), SRCategory("CatLayout"), SRDescription("ControlHeightDescr")]
		public int Height
		{
			get
			{
				return this.height;
			}
			set
			{
				this.SetBounds(this.x, this.y, this.width, value, BoundsSpecified.Height);
			}
		}

		internal bool HostedInWin32DialogManager
		{
			get
			{
				if (!this.GetState(16777216))
				{
					Control topMostParent = this.TopMostParent;
					if (this != topMostParent)
					{
						this.SetState(33554432, topMostParent.HostedInWin32DialogManager);
					}
					else
					{
						IntPtr intPtr = UnsafeNativeMethods.GetParent(new HandleRef(this, this.Handle));
						IntPtr handle = intPtr;
						StringBuilder stringBuilder = new StringBuilder(32);
						this.SetState(33554432, false);
						while (intPtr != IntPtr.Zero)
						{
							int className = UnsafeNativeMethods.GetClassName(new HandleRef(null, handle), null, 0);
							if (className > stringBuilder.Capacity)
							{
								stringBuilder.Capacity = className + 5;
							}
							HandleRef arg_8D_0 = new HandleRef(null, handle);
							StringBuilder expr_87 = stringBuilder;
							UnsafeNativeMethods.GetClassName(arg_8D_0, expr_87, expr_87.Capacity);
							if (stringBuilder.ToString() == "#32770")
							{
								this.SetState(33554432, true);
								break;
							}
							handle = intPtr;
							intPtr = UnsafeNativeMethods.GetParent(new HandleRef(null, intPtr));
						}
					}
					this.SetState(16777216, true);
				}
				return this.GetState(33554432);
			}
		}

		/// <summary>Получает значение, показывающее, имеется ли у элемента управления сопоставленный с ним дескриптор.</summary>
		/// <returns>Значение true, если элементу управления был назначен дескриптор; в противном случае — значение false.</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlHandleCreatedDescr")]
		public bool IsHandleCreated
		{
			get
			{
				return this.window.Handle != IntPtr.Zero;
			}
		}

		internal bool IsLayoutSuspended
		{
			get
			{
				return this.layoutSuspendCount > 0;
			}
		}

		internal bool IsWindowObscured
		{
			get
			{
				if (!this.IsHandleCreated || !this.Visible)
				{
					return false;
				}
				bool result = false;
				NativeMethods.RECT rECT = default(NativeMethods.RECT);
				Control parentInternal = this.ParentInternal;
				if (parentInternal != null)
				{
					while (parentInternal.ParentInternal != null)
					{
						parentInternal = parentInternal.ParentInternal;
					}
				}
				UnsafeNativeMethods.GetWindowRect(new HandleRef(this, this.Handle), ref rECT);
				Region region = new Region(Rectangle.FromLTRB(rECT.left, rECT.top, rECT.right, rECT.bottom));
				try
				{
					IntPtr handle;
					if (parentInternal != null)
					{
						handle = parentInternal.Handle;
					}
					else
					{
						handle = this.Handle;
					}
					IntPtr handle2 = handle;
					IntPtr intPtr;
					while ((intPtr = UnsafeNativeMethods.GetWindow(new HandleRef(null, handle2), 3)) != IntPtr.Zero)
					{
						UnsafeNativeMethods.GetWindowRect(new HandleRef(null, intPtr), ref rECT);
						Rectangle rect = Rectangle.FromLTRB(rECT.left, rECT.top, rECT.right, rECT.bottom);
						if (SafeNativeMethods.IsWindowVisible(new HandleRef(null, intPtr)))
						{
							region.Exclude(rect);
						}
						handle2 = intPtr;
					}
					Graphics graphics = this.CreateGraphics();
					try
					{
						result = region.IsEmpty(graphics);
					}
					finally
					{
						graphics.Dispose();
					}
				}
				finally
				{
					region.Dispose();
				}
				return result;
			}
		}

		internal IntPtr InternalHandle
		{
			get
			{
				if (!this.IsHandleCreated)
				{
					return IntPtr.Zero;
				}
				return this.Handle;
			}
		}

		/// <summary>Получает значение, показывающее, следует ли вызывающему оператору обращаться к методу invoke во время вызовов метода из элемента управления, так как вызывающий оператор находится не в том потоке, в котором был создан элемент управления.</summary>
		/// <returns>Значение true, если свойство <see cref="P:System.Windows.Forms.Control.Handle" /> элемента управления было создано не в вызывающем потоке, а в другом (показывает, что необходимо вызвать элемент управления через метод invoke); в противном случае — значение false.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlInvokeRequiredDescr")]
		public bool InvokeRequired
		{
			get
			{
				bool result;
				using (new Control.MultithreadSafeCallScope())
				{
					HandleRef hWnd;
					if (this.IsHandleCreated)
					{
						hWnd = new HandleRef(this, this.Handle);
					}
					else
					{
						Control control = this.FindMarshalingControl();
						if (!control.IsHandleCreated)
						{
							result = false;
							return result;
						}
						hWnd = new HandleRef(control, control.Handle);
					}
					int num;
					int arg_53_0 = SafeNativeMethods.GetWindowThreadProcessId(hWnd, out num);
					int currentThreadId = SafeNativeMethods.GetCurrentThreadId();
					result = (arg_53_0 != currentThreadId);
				}
				return result;
			}
		}

		/// <summary>Возвращает или задает значение, показывающее, является ли элемент управления видимым для приложений со специальными возможностями.</summary>
		/// <returns>Значение true, если элемент управления является видимым для приложений со специальными возможностями; в противном случае — значение false.</returns>
		/// <filterpriority>2</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatBehavior"), SRDescription("ControlIsAccessibleDescr")]
		public bool IsAccessible
		{
			get
			{
				return this.GetState(1048576);
			}
			set
			{
				this.SetState(1048576, value);
			}
		}

		internal bool IsActiveX
		{
			get
			{
				return this.GetState2(1024);
			}
		}

		internal virtual bool IsContainerControl
		{
			get
			{
				return false;
			}
		}

		internal bool IsIEParent
		{
			get
			{
				return this.IsActiveX && this.ActiveXInstance.IsIE;
			}
		}

		/// <summary>Получает значение, показывающее, отображается ли зеркально элемент управления.</summary>
		/// <returns>Значение true, если данный элемент управления отображается зеркально; в противном случае — значение false.</returns>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatLayout"), SRDescription("IsMirroredDescr")]
		public bool IsMirrored
		{
			get
			{
				if (!this.IsHandleCreated)
				{
					CreateParams createParams = this.CreateParams;
					this.SetState(1073741824, (createParams.ExStyle & 4194304) != 0);
				}
				return this.GetState(1073741824);
			}
		}

		internal virtual bool IsMnemonicsListenerAxSourced
		{
			get
			{
				return false;
			}
		}

		/// <summary>Возвращает или задает расстояние (в точках) между левой границей элемента управления и левой границей клиентской области его контейнера.</summary>
		/// <returns>Объект <see cref="T:System.Int32" />, представляющий расстояние (в точках) между левой границей элемента управления и левой границей клиентской области его контейнера.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Always), SRCategory("CatLayout"), SRDescription("ControlLeftDescr")]
		public int Left
		{
			get
			{
				return this.x;
			}
			set
			{
				this.SetBounds(value, this.y, this.width, this.height, BoundsSpecified.X);
			}
		}

		/// <summary>Возвращает или задает координаты левого верхнего угла элемента управления относительно левого верхнего угла контейнера.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Point" />, представляющий левый верхний угол элемента управления относительно левого верхнего угла контейнера. </returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Localizable(true), SRCategory("CatLayout"), SRDescription("ControlLocationDescr")]
		public Point Location
		{
			get
			{
				return new Point(this.x, this.y);
			}
			set
			{
				this.SetBounds(value.X, value.Y, this.width, this.height, BoundsSpecified.Location);
			}
		}

		/// <summary>Возвращает или задает пустое пространство между элементами управления.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.Padding" />, представляющий пустое пространство между элементами управления.</returns>
		/// <filterpriority>2</filterpriority>
		[Localizable(true), SRCategory("CatLayout"), SRDescription("ControlMarginDescr")]
		public Padding Margin
		{
			get
			{
				return CommonProperties.GetMargin(this);
			}
			set
			{
				value = LayoutUtils.ClampNegativePaddingToZero(value);
				if (value != this.Margin)
				{
					CommonProperties.SetMargin(this, value);
					this.OnMarginChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>Возвращает или задает размер, являющийся верхней границей, которую может указать метод <see cref="M:System.Windows.Forms.Control.GetPreferredSize(System.Drawing.Size)" />.</summary>
		/// <returns>Упорядоченная пара типа <see cref="T:System.Drawing.Size" />, представляющая ширину и высоту прямоугольника.</returns>
		/// <filterpriority>1</filterpriority>
		[AmbientValue(typeof(Size), "0, 0"), Localizable(true), SRCategory("CatLayout"), SRDescription("ControlMaximumSizeDescr")]
		public virtual Size MaximumSize
		{
			get
			{
				return CommonProperties.GetMaximumSize(this, this.DefaultMaximumSize);
			}
			set
			{
				if (value == Size.Empty)
				{
					CommonProperties.ClearMaximumSize(this);
					return;
				}
				if (value != this.MaximumSize)
				{
					CommonProperties.SetMaximumSize(this, value);
				}
			}
		}

		/// <summary>Возвращает или задает размер, являющийся нижней границей, которую может указать метод <see cref="M:System.Windows.Forms.Control.GetPreferredSize(System.Drawing.Size)" />.</summary>
		/// <returns>Упорядоченная пара типа <see cref="T:System.Drawing.Size" />, представляющая ширину и высоту прямоугольника.</returns>
		/// <filterpriority>1</filterpriority>
		[Localizable(true), SRCategory("CatLayout"), SRDescription("ControlMinimumSizeDescr")]
		public virtual Size MinimumSize
		{
			get
			{
				return CommonProperties.GetMinimumSize(this, this.DefaultMinimumSize);
			}
			set
			{
				if (value != this.MinimumSize)
				{
					CommonProperties.SetMinimumSize(this, value);
				}
			}
		}

		/// <summary>Получает значение, показывающее, какие из клавиш (SHIFT, CTRL и ALT) нажаты в данный момент.</summary>
		/// <returns>Битовая комбинация значений <see cref="T:System.Windows.Forms.Keys" />.Значением по умолчанию является <see cref="F:System.Windows.Forms.Keys.None" />.</returns>
		/// <filterpriority>2</filterpriority>
		public static Keys ModifierKeys
		{
			get
			{
				Keys keys = Keys.None;
				if (UnsafeNativeMethods.GetKeyState(16) < 0)
				{
					keys |= Keys.Shift;
				}
				if (UnsafeNativeMethods.GetKeyState(17) < 0)
				{
					keys |= Keys.Control;
				}
				if (UnsafeNativeMethods.GetKeyState(18) < 0)
				{
					keys |= Keys.Alt;
				}
				return keys;
			}
		}

		/// <summary>Получает значение, показывающее, какая из кнопок мыши нажата в данный момент.</summary>
		/// <returns>Битовая комбинация значений перечисления <see cref="T:System.Windows.Forms.MouseButtons" />.Значением по умолчанию является <see cref="F:System.Windows.Forms.MouseButtons.None" />.</returns>
		/// <filterpriority>2</filterpriority>
		public static MouseButtons MouseButtons
		{
			get
			{
				MouseButtons mouseButtons = MouseButtons.None;
				if (UnsafeNativeMethods.GetKeyState(1) < 0)
				{
					mouseButtons |= MouseButtons.Left;
				}
				if (UnsafeNativeMethods.GetKeyState(2) < 0)
				{
					mouseButtons |= MouseButtons.Right;
				}
				if (UnsafeNativeMethods.GetKeyState(4) < 0)
				{
					mouseButtons |= MouseButtons.Middle;
				}
				if (UnsafeNativeMethods.GetKeyState(5) < 0)
				{
					mouseButtons |= MouseButtons.XButton1;
				}
				if (UnsafeNativeMethods.GetKeyState(6) < 0)
				{
					mouseButtons |= MouseButtons.XButton2;
				}
				return mouseButtons;
			}
		}

		/// <summary>Получает позицию указателя мыши в экранных координатах.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Point" /> содержит координаты указателя мыши относительно левого верхнего угла экрана.</returns>
		/// <filterpriority>2</filterpriority>
		public static Point MousePosition
		{
			get
			{
				NativeMethods.POINT pOINT = new NativeMethods.POINT();
				UnsafeNativeMethods.GetCursorPos(pOINT);
				return new Point(pOINT.x, pOINT.y);
			}
		}

		/// <summary>Возвращает или задает имя элемента управления.</summary>
		/// <returns>Имя элемента управления.Значением по умолчанию является пустая строка ("").</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false)]
		public string Name
		{
			get
			{
				string text = (string)this.Properties.GetObject(Control.PropName);
				if (string.IsNullOrEmpty(text))
				{
					if (this.Site != null)
					{
						text = this.Site.Name;
					}
					if (text == null)
					{
						text = "";
					}
				}
				return text;
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					this.Properties.SetObject(Control.PropName, null);
					return;
				}
				this.Properties.SetObject(Control.PropName, value);
			}
		}

		/// <summary>Возвращает или задает родительский контейнер элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.Control" />, представляющий родительский элемент управления или контейнерный элемент управления элемента управления.</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), SRCategory("CatBehavior"), SRDescription("ControlParentDescr")]
		public Control Parent
		{
			get
			{
				IntSecurity.GetParent.Demand();
				return this.ParentInternal;
			}
			set
			{
				this.ParentInternal = value;
			}
		}

		internal virtual Control ParentInternal
		{
			get
			{
				return this.parent;
			}
			set
			{
				if (this.parent != value)
				{
					if (value != null)
					{
						value.Controls.Add(this);
						return;
					}
					this.parent.Controls.Remove(this);
				}
			}
		}

		/// <summary>Получает имя продукта сборки, содержащей элемент управления.</summary>
		/// <returns>Имя продукта сборки, содержащей элемент управления.</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlProductNameDescr")]
		public string ProductName
		{
			get
			{
				return this.VersionInfo.ProductName;
			}
		}

		/// <summary>Получает версию сборки, содержащую элемент управления.</summary>
		/// <returns>Версия файла сборки, содержащая элемент управления.</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRDescription("ControlProductVersionDescr")]
		public string ProductVersion
		{
			get
			{
				return this.VersionInfo.ProductVersion;
			}
		}

		internal PropertyStore Properties
		{
			get
			{
				return this.propertyStore;
			}
		}

		internal Color RawBackColor
		{
			get
			{
				return this.Properties.GetColor(Control.PropBackColor);
			}
		}

		/// <summary>Получает значение, показывающее, осуществляется ли в данный момент повторное создание дескриптора элементом управления.</summary>
		/// <returns>Значение true, если в данный момент осуществляется повторное создание дескриптора элементом управления; в противном случае — значение false.</returns>
		/// <filterpriority>2</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatBehavior"), SRDescription("ControlRecreatingHandleDescr")]
		public bool RecreatingHandle
		{
			get
			{
				return (this.state & 16) != 0;
			}
		}

		private Control ReflectParent
		{
			get
			{
				return this.reflectParent;
			}
			set
			{
				if (value != null)
				{
					value.AddReflectChild();
				}
				Control control = this.ReflectParent;
				this.reflectParent = value;
				if (control != null)
				{
					control.RemoveReflectChild();
				}
			}
		}

		/// <summary>Возвращает или задает область окна, сопоставленную с элементом управления.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Region" /> окна, сопоставленный с элементом управления.</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatLayout"), SRDescription("ControlRegionDescr")]
		public Region Region
		{
			get
			{
				return (Region)this.Properties.GetObject(Control.PropRegion);
			}
			set
			{
				if (this.GetState(524288))
				{
					IntSecurity.ChangeWindowRegionForTopLevel.Demand();
				}
				Region region = this.Region;
				if (region != value)
				{
					this.Properties.SetObject(Control.PropRegion, value);
					if (region != null)
					{
						region.Dispose();
					}
					if (this.IsHandleCreated)
					{
						IntPtr intPtr = IntPtr.Zero;
						try
						{
							if (value != null)
							{
								intPtr = this.GetHRgn(value);
							}
							if (this.IsActiveX)
							{
								intPtr = this.ActiveXMergeRegion(intPtr);
							}
							if (UnsafeNativeMethods.SetWindowRgn(new HandleRef(this, this.Handle), new HandleRef(this, intPtr), SafeNativeMethods.IsWindowVisible(new HandleRef(this, this.Handle))) != 0)
							{
								intPtr = IntPtr.Zero;
							}
						}
						finally
						{
							if (intPtr != IntPtr.Zero)
							{
								SafeNativeMethods.DeleteObject(new HandleRef(null, intPtr));
							}
						}
					}
					this.OnRegionChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>Это свойство устарело.</summary>
		/// <returns>Значение true, если элемент управления прорисовывается справа налево; в противном случае — значение false. Значение по умолчанию — false.</returns>
		[Obsolete("This property has been deprecated. Please use RightToLeft instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		protected internal bool RenderRightToLeft
		{
			get
			{
				return true;
			}
		}

		internal bool RenderTransparent
		{
			get
			{
				return this.GetStyle(ControlStyles.SupportsTransparentBackColor) && this.BackColor.A < 255;
			}
		}

		internal virtual bool RenderTransparencyWithVisualStyles
		{
			get
			{
				return false;
			}
		}

		internal BoundsSpecified RequiredScaling
		{
			get
			{
				if ((this.requiredScaling & 16) != 0)
				{
					return (BoundsSpecified)(this.requiredScaling & 15);
				}
				return BoundsSpecified.None;
			}
			set
			{
				byte b = this.requiredScaling & 16;
				this.requiredScaling = (byte)((value & BoundsSpecified.All) | (BoundsSpecified)b);
			}
		}

		internal bool RequiredScalingEnabled
		{
			get
			{
				return (this.requiredScaling & 16) > 0;
			}
			set
			{
				byte b = this.requiredScaling & 15;
				this.requiredScaling = b;
				if (value)
				{
					this.requiredScaling |= 16;
				}
			}
		}

		/// <summary>Возвращает или задает значение, указывающее, перерисовывается ли элемент управления при изменении размеров.</summary>
		/// <returns>Значение true, если элемент управления перерисовывается при изменении размеров; в противном случае — значение false.</returns>
		[SRDescription("ControlResizeRedrawDescr")]
		protected bool ResizeRedraw
		{
			get
			{
				return this.GetStyle(ControlStyles.ResizeRedraw);
			}
			set
			{
				this.SetStyle(ControlStyles.ResizeRedraw, value);
			}
		}

		/// <summary>Получает расстояние (в точках) между правой границей элемента управления и левой границей клиентской области его контейнера.</summary>
		/// <returns>Объект <see cref="T:System.Int32" />, представляющий расстояние (в точках) между правой границей элемента управления и левой границей клиентской области его контейнера.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatLayout"), SRDescription("ControlRightDescr")]
		public int Right
		{
			get
			{
				return this.x + this.width;
			}
		}

		/// <summary>Возвращает или задает значение, показывающее, выровнены ли компоненты элемента управления для поддержки языков, использующих шрифты с написанием справа налево.</summary>
		/// <returns>Одно из значений <see cref="T:System.Windows.Forms.RightToLeft" />.Значение по умолчанию — <see cref="F:System.Windows.Forms.RightToLeft.Inherit" />.</returns>
		/// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">Присваиваемое значение не относится к значениям <see cref="T:System.Windows.Forms.RightToLeft" />. </exception>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[AmbientValue(RightToLeft.Inherit), Localizable(true), SRCategory("CatAppearance"), SRDescription("ControlRightToLeftDescr")]
		public virtual RightToLeft RightToLeft
		{
			get
			{
				bool flag;
				int num = this.Properties.GetInteger(Control.PropRightToLeft, out flag);
				if (!flag)
				{
					num = 2;
				}
				if (num == 2)
				{
					Control parentInternal = this.ParentInternal;
					if (parentInternal != null)
					{
						num = (int)parentInternal.RightToLeft;
					}
					else
					{
						num = (int)this.DefaultRightToLeft;
					}
				}
				return (RightToLeft)num;
			}
			set
			{
				if (!ClientUtils.IsEnumValid(value, (int)value, 0, 2))
				{
					throw new InvalidEnumArgumentException("RightToLeft", (int)value, typeof(RightToLeft));
				}
				RightToLeft arg_59_0 = this.RightToLeft;
				if (this.Properties.ContainsInteger(Control.PropRightToLeft) || value != RightToLeft.Inherit)
				{
					this.Properties.SetInteger(Control.PropRightToLeft, (int)value);
				}
				if (arg_59_0 != this.RightToLeft)
				{
					using (new LayoutTransaction(this, this, PropertyNames.RightToLeft))
					{
						this.OnRightToLeftChanged(EventArgs.Empty);
					}
				}
			}
		}

		/// <summary>Получает значение, определяющее масштабирование дочерних элементов управления. </summary>
		/// <returns>Значение true, если дочерние элементы управления будут масштабироваться при вызове метода <see cref="M:System.Windows.Forms.Control.Scale(System.Single)" /> в этом элементе управления; в противном случае — значение false.Значение по умолчанию — true.</returns>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual bool ScaleChildren
		{
			get
			{
				return true;
			}
		}

		/// <summary>Возвращает или задает подложку элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.ComponentModel.ISite" />, сопоставленный с объектом <see cref="T:System.Windows.Forms.Control" /> (при наличии такового).</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public override ISite Site
		{
			get
			{
				return base.Site;
			}
			set
			{
				AmbientProperties arg_22_0 = this.AmbientPropertiesService;
				AmbientProperties ambientProperties = null;
				if (value != null)
				{
					ambientProperties = (AmbientProperties)value.GetService(typeof(AmbientProperties));
				}
				if (arg_22_0 != ambientProperties)
				{
					bool arg_8A_0 = !this.Properties.ContainsObject(Control.PropFont);
					bool flag = !this.Properties.ContainsObject(Control.PropBackColor);
					bool flag2 = !this.Properties.ContainsObject(Control.PropForeColor);
					bool flag3 = !this.Properties.ContainsObject(Control.PropCursor);
					Font font = null;
					Color color = Color.Empty;
					Color color2 = Color.Empty;
					Cursor cursor = null;
					if (arg_8A_0)
					{
						font = this.Font;
					}
					if (flag)
					{
						color = this.BackColor;
					}
					if (flag2)
					{
						color2 = this.ForeColor;
					}
					if (flag3)
					{
						cursor = this.Cursor;
					}
					this.Properties.SetObject(Control.PropAmbientPropertiesService, ambientProperties);
					base.Site = value;
					if (arg_8A_0 && !font.Equals(this.Font))
					{
						this.OnFontChanged(EventArgs.Empty);
					}
					if (flag2 && !color2.Equals(this.ForeColor))
					{
						this.OnForeColorChanged(EventArgs.Empty);
					}
					if (flag && !color.Equals(this.BackColor))
					{
						this.OnBackColorChanged(EventArgs.Empty);
					}
					if (flag3 && cursor.Equals(this.Cursor))
					{
						this.OnCursorChanged(EventArgs.Empty);
						return;
					}
				}
				else
				{
					base.Site = value;
				}
			}
		}

		/// <summary>Возвращает или задает высоту и ширину элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Size" /> представляет высоту и ширину элемента управления (в точках).</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Localizable(true), SRCategory("CatLayout"), SRDescription("ControlSizeDescr")]
		public Size Size
		{
			get
			{
				return new Size(this.width, this.height);
			}
			set
			{
				this.SetBounds(this.x, this.y, value.Width, value.Height, BoundsSpecified.Size);
			}
		}

		/// <summary>Возвращает или задает последовательность перехода элемента управления внутри контейнера.</summary>
		/// <returns>Значение индекса элемента управления из набора элементов управления в его контейнере.Элементы управления в контейнере включены в последовательность табуляции.</returns>
		/// <filterpriority>1</filterpriority>
		[Localizable(true), MergableProperty(false), SRCategory("CatBehavior"), SRDescription("ControlTabIndexDescr")]
		public int TabIndex
		{
			get
			{
				if (this.tabIndex != -1)
				{
					return this.tabIndex;
				}
				return 0;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException("TabIndex", SR.GetString("InvalidLowBoundArgumentEx", new object[]
					{
						"TabIndex",
						value.ToString(CultureInfo.CurrentCulture),
						0.ToString(CultureInfo.CurrentCulture)
					}));
				}
				if (this.tabIndex != value)
				{
					this.tabIndex = value;
					this.OnTabIndexChanged(EventArgs.Empty);
				}
			}
		}

		/// <summary>Получает или задает значение, показывающее, можно ли перевести фокус на данный элемент управления при помощи клавиши TAB.</summary>
		/// <returns>Значение true, если с помощью клавиши TAB можно перевести фокус на элемент управления; в противном случае — значение false.По умолчанию установлено значение — true.ПримечаниеЭто свойство будет всегда возвращать значение true для экземпляра класса <see cref="T:System.Windows.Forms.Form" />.</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[DefaultValue(true), DispId(-516), SRCategory("CatBehavior"), SRDescription("ControlTabStopDescr")]
		public bool TabStop
		{
			get
			{
				return this.TabStopInternal;
			}
			set
			{
				if (this.TabStop != value)
				{
					this.TabStopInternal = value;
					if (this.IsHandleCreated)
					{
						this.SetWindowStyle(65536, value);
					}
					this.OnTabStopChanged(EventArgs.Empty);
				}
			}
		}

		internal bool TabStopInternal
		{
			get
			{
				return (this.state & 8) != 0;
			}
			set
			{
				if (this.TabStopInternal != value)
				{
					this.SetState(8, value);
				}
			}
		}

		/// <summary>Возвращает или задает объект, содержащий данные элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.Object" />, содержащий данные элемента управления. По умолчанию задано значение null.</returns>
		/// <filterpriority>2</filterpriority>
		[Bindable(true), DefaultValue(null), Localizable(false), TypeConverter(typeof(StringConverter)), SRCategory("CatData"), SRDescription("ControlTagDescr")]
		public object Tag
		{
			get
			{
				return this.Properties.GetObject(Control.PropUserData);
			}
			set
			{
				this.Properties.SetObject(Control.PropUserData, value);
			}
		}

		/// <summary>Получает или задает текст, сопоставленный с этим элементом управления.</summary>
		/// <returns>Текст, сопоставленный с этим элементом управления.</returns>
		/// <filterpriority>1</filterpriority>
		[Bindable(true), Localizable(true), DispId(-517), SRCategory("CatAppearance"), SRDescription("ControlTextDescr")]
		public virtual string Text
		{
			get
			{
				if (!this.CacheTextInternal)
				{
					return this.WindowText;
				}
				if (this.text != null)
				{
					return this.text;
				}
				return "";
			}
			set
			{
				if (value == null)
				{
					value = "";
				}
				if (value == this.Text)
				{
					return;
				}
				if (this.CacheTextInternal)
				{
					this.text = value;
				}
				this.WindowText = value;
				this.OnTextChanged(EventArgs.Empty);
				if (this.IsMnemonicsListenerAxSourced)
				{
					for (Control control = this; control != null; control = control.ParentInternal)
					{
						Control.ActiveXImpl activeXImpl = (Control.ActiveXImpl)control.Properties.GetObject(Control.PropActiveXImpl);
						if (activeXImpl != null)
						{
							activeXImpl.UpdateAccelTable();
							return;
						}
					}
				}
			}
		}

		/// <summary>Возвращает или задает расстояние (в точках) между верхней границей элемента управления и верхней границей клиентской области его контейнера.</summary>
		/// <returns>Объект <see cref="T:System.Int32" />, представляющий расстояние (в точках) между нижней границей элемента управления и верхней границей клиентской области контейнера.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Always), SRCategory("CatLayout"), SRDescription("ControlTopDescr")]
		public int Top
		{
			get
			{
				return this.y;
			}
			set
			{
				this.SetBounds(this.x, value, this.width, this.height, BoundsSpecified.Y);
			}
		}

		/// <summary>Получает родительский элемент управления, не имеющий другого родительского элемента управления Windows Forms.Как правило, им является внешний объект <see cref="T:System.Windows.Forms.Form" />, в котором содержится элемент управления.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.Control" /> представляет элемент управления верхнего уровня, содержащий текущий элемент управления.</returns>
		/// <filterpriority>2</filterpriority>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced), SRCategory("CatBehavior"), SRDescription("ControlTopLevelControlDescr")]
		public Control TopLevelControl
		{
			get
			{
				IntSecurity.GetParent.Demand();
				return this.TopLevelControlInternal;
			}
		}

		internal Control TopLevelControlInternal
		{
			get
			{
				Control control = this;
				while (control != null && !control.GetTopLevel())
				{
					control = control.ParentInternal;
				}
				return control;
			}
		}

		internal Control TopMostParent
		{
			get
			{
				Control control = this;
				while (control.ParentInternal != null)
				{
					control = control.ParentInternal;
				}
				return control;
			}
		}

		private BufferedGraphicsContext BufferContext
		{
			get
			{
				return BufferedGraphicsManager.Current;
			}
		}

		/// <summary>Получает значение, указывающее, имеет ли пользовательский интерфейс соответствующее состояние, при котором отображаются или скрываются сочетания клавиш.</summary>
		/// <returns>Значение true, если сочетания клавиш являются видимыми; в противном случае — значение false.</returns>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced)]
		protected internal virtual bool ShowKeyboardCues
		{
			get
			{
				if (!this.IsHandleCreated || base.DesignMode)
				{
					return true;
				}
				if ((this.uiCuesState & 240) == 0)
				{
					if (SystemInformation.MenuAccessKeysUnderlined)
					{
						this.uiCuesState |= 32;
					}
					else
					{
						int num = 196608;
						this.uiCuesState |= 16;
						UnsafeNativeMethods.SendMessage(new HandleRef(this.TopMostParent, this.TopMostParent.Handle), 295, (IntPtr)(num | 1), IntPtr.Zero);
					}
				}
				return (this.uiCuesState & 240) == 32;
			}
		}

		/// <summary>Получает значение, показывающее, должен ли элемент управления отображать прямоугольники фокуса.</summary>
		/// <returns>Значение true, если элемент управления должен отображать прямоугольники фокуса; в противном случае — значение false.</returns>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Advanced)]
		protected internal virtual bool ShowFocusCues
		{
			get
			{
				if (!this.IsHandleCreated)
				{
					return true;
				}
				if ((this.uiCuesState & 15) == 0)
				{
					if (SystemInformation.MenuAccessKeysUnderlined)
					{
						this.uiCuesState |= 2;
					}
					else
					{
						this.uiCuesState |= 1;
						int num = 196608;
						UnsafeNativeMethods.SendMessage(new HandleRef(this.TopMostParent, this.TopMostParent.Handle), 295, (IntPtr)(num | 1), IntPtr.Zero);
					}
				}
				return (this.uiCuesState & 15) == 2;
			}
		}

		internal virtual int ShowParams
		{
			get
			{
				return 5;
			}
		}

		/// <summary>Возвращает или задает значение, указывающее, следует ли использовать курсор ожидания для текущего элемента управления и всех дочерних элементов управления.</summary>
		/// <returns>Значение true, чтобы использовать курсор ожидания для текущего элемента управления и всех дочерних элементов управления; в противном случае — значение false.Значение по умолчанию — false.</returns>
		/// <filterpriority>2</filterpriority>
		[Browsable(true), DefaultValue(false), EditorBrowsable(EditorBrowsableState.Always), SRCategory("CatAppearance"), SRDescription("ControlUseWaitCursorDescr")]
		public bool UseWaitCursor
		{
			get
			{
				return this.GetState(1024);
			}
			set
			{
				if (this.GetState(1024) != value)
				{
					this.SetState(1024, value);
					Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
					if (controlCollection != null)
					{
						for (int i = 0; i < controlCollection.Count; i++)
						{
							controlCollection[i].UseWaitCursor = value;
						}
					}
				}
			}
		}

		internal bool UseCompatibleTextRenderingInt
		{
			get
			{
				if (this.Properties.ContainsInteger(Control.PropUseCompatibleTextRendering))
				{
					bool flag;
					int integer = this.Properties.GetInteger(Control.PropUseCompatibleTextRendering, out flag);
					if (flag)
					{
						return integer == 1;
					}
				}
				return Control.UseCompatibleTextRenderingDefault;
			}
			set
			{
				if (this.SupportsUseCompatibleTextRendering && this.UseCompatibleTextRenderingInt != value)
				{
					this.Properties.SetInteger(Control.PropUseCompatibleTextRendering, value ? 1 : 0);
					LayoutTransaction.DoLayoutIf(this.AutoSize, this.ParentInternal, this, PropertyNames.UseCompatibleTextRendering);
					this.Invalidate();
				}
			}
		}

		internal virtual bool SupportsUseCompatibleTextRendering
		{
			get
			{
				return false;
			}
		}

		private Control.ControlVersionInfo VersionInfo
		{
			get
			{
				Control.ControlVersionInfo controlVersionInfo = (Control.ControlVersionInfo)this.Properties.GetObject(Control.PropControlVersionInfo);
				if (controlVersionInfo == null)
				{
					controlVersionInfo = new Control.ControlVersionInfo(this);
					this.Properties.SetObject(Control.PropControlVersionInfo, controlVersionInfo);
				}
				return controlVersionInfo;
			}
		}

		/// <summary>Получает или задает значение, указывающее, отображаются ли элемент управления и все его дочерние элементы управления.</summary>
		/// <returns>Значение true, если элемент управления и все его дочерние элементы управления отображаются; в противном случае — значение false.Значение по умолчанию — true.</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Localizable(true), SRCategory("CatBehavior"), SRDescription("ControlVisibleDescr")]
		public bool Visible
		{
			get
			{
				return this.GetVisibleCore();
			}
			set
			{
				this.SetVisibleCore(value);
			}
		}

		/// <summary>Возвращает или задает ширину элемента управления.</summary>
		/// <returns>Ширина элемента управления (в точках).</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Always), SRCategory("CatLayout"), SRDescription("ControlWidthDescr")]
		public int Width
		{
			get
			{
				return this.width;
			}
			set
			{
				this.SetBounds(this.x, this.y, value, this.height, BoundsSpecified.Width);
			}
		}

		private int WindowExStyle
		{
			get
			{
				return (int)((long)UnsafeNativeMethods.GetWindowLong(new HandleRef(this, this.Handle), -20));
			}
			set
			{
				UnsafeNativeMethods.SetWindowLong(new HandleRef(this, this.Handle), -20, new HandleRef(null, (IntPtr)value));
			}
		}

		internal int WindowStyle
		{
			get
			{
				return (int)((long)UnsafeNativeMethods.GetWindowLong(new HandleRef(this, this.Handle), -16));
			}
			set
			{
				UnsafeNativeMethods.SetWindowLong(new HandleRef(this, this.Handle), -16, new HandleRef(null, (IntPtr)value));
			}
		}

		/// <summary>Данное свойство не относится к этому классу.</summary>
		/// <returns>Значение <see cref="T:System.Windows.Forms.IWindowTarget" />.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode" />
		/// </PermissionSet>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never), SRCategory("CatBehavior"), SRDescription("ControlWindowTargetDescr")]
		public IWindowTarget WindowTarget
		{
			[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			get
			{
				return this.window.WindowTarget;
			}
			[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
			set
			{
				this.window.WindowTarget = value;
			}
		}

		internal virtual string WindowText
		{
			get
			{
				if (this.IsHandleCreated)
				{
					string result;
					using (new Control.MultithreadSafeCallScope())
					{
						int num = SafeNativeMethods.GetWindowTextLength(new HandleRef(this.window, this.Handle));
						if (SystemInformation.DbcsEnabled)
						{
							num = num * 2 + 1;
						}
						StringBuilder stringBuilder = new StringBuilder(num + 1);
						HandleRef arg_68_0 = new HandleRef(this.window, this.Handle);
						StringBuilder expr_62 = stringBuilder;
						UnsafeNativeMethods.GetWindowText(arg_68_0, expr_62, expr_62.Capacity);
						result = stringBuilder.ToString();
					}
					return result;
				}
				if (this.text == null)
				{
					return "";
				}
				return this.text;
			}
			set
			{
				if (value == null)
				{
					value = "";
				}
				if (!this.WindowText.Equals(value))
				{
					if (this.IsHandleCreated)
					{
						UnsafeNativeMethods.SetWindowText(new HandleRef(this.window, this.Handle), value);
						return;
					}
					if (value.Length == 0)
					{
						this.text = null;
						return;
					}
					this.text = value;
				}
			}
		}

		/// <summary>Получает размер прямоугольной области, в которую может поместиться элемент управления.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Size" />, содержащий значения высоты и ширины в точках.</returns>
		/// <filterpriority>1</filterpriority>
		[Browsable(false)]
		public Size PreferredSize
		{
			get
			{
				return this.GetPreferredSize(Size.Empty);
			}
		}

		/// <summary>Возвращает или задает заполнение в элементе управления.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.Padding" />, представляющий параметры внутренних зазоров элемента управления.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[Localizable(true), SRCategory("CatLayout"), SRDescription("ControlPaddingDescr")]
		public Padding Padding
		{
			get
			{
				return CommonProperties.GetPadding(this, this.DefaultPadding);
			}
			set
			{
				if (value != this.Padding)
				{
					CommonProperties.SetPadding(this, value);
					this.SetState(8388608, true);
					using (new LayoutTransaction(this.ParentInternal, this, PropertyNames.Padding))
					{
						this.OnPaddingChanged(EventArgs.Empty);
					}
					if (this.GetState(8388608))
					{
						LayoutTransaction.DoLayout(this, this, PropertyNames.Padding);
					}
				}
			}
		}

		internal ContainerControl ParentContainerControl
		{
			get
			{
				for (Control parentInternal = this.ParentInternal; parentInternal != null; parentInternal = parentInternal.ParentInternal)
				{
					if (parentInternal is ContainerControl)
					{
						return parentInternal as ContainerControl;
					}
				}
				return null;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal bool ShouldAutoValidate
		{
			get
			{
				return Control.GetAutoValidateForControl(this) > AutoValidate.Disable;
			}
		}

		ArrangedElementCollection IArrangedElement.Children
		{
			get
			{
				Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
				if (controlCollection == null)
				{
					return ArrangedElementCollection.Empty;
				}
				return controlCollection;
			}
		}

		IArrangedElement IArrangedElement.Container
		{
			get
			{
				return this.ParentInternal;
			}
		}

		bool IArrangedElement.ParticipatesInLayout
		{
			get
			{
				return this.GetState(2);
			}
		}

		PropertyStore IArrangedElement.Properties
		{
			get
			{
				return this.Properties;
			}
		}

		internal ImeMode CachedImeMode
		{
			get
			{
				bool flag;
				ImeMode imeMode = (ImeMode)this.Properties.GetInteger(Control.PropImeMode, out flag);
				if (!flag)
				{
					imeMode = this.DefaultImeMode;
				}
				if (imeMode == ImeMode.Inherit)
				{
					Control parentInternal = this.ParentInternal;
					if (parentInternal != null)
					{
						imeMode = parentInternal.CachedImeMode;
					}
					else
					{
						imeMode = ImeMode.NoControl;
					}
				}
				return imeMode;
			}
			set
			{
				this.Properties.SetInteger(Control.PropImeMode, (int)value);
			}
		}

		/// <summary>Получает значение, указывающее, можно ли для свойства <see cref="P:System.Windows.Forms.Control.ImeMode" /> установить активное значение с целью включения поддержки IME.</summary>
		/// <returns>Значение true во всех случаях.</returns>
		protected virtual bool CanEnableIme
		{
			get
			{
				return this.ImeSupported;
			}
		}

		internal ImeMode CurrentImeContextMode
		{
			get
			{
				if (this.IsHandleCreated)
				{
					return ImeContext.GetImeMode(this.Handle);
				}
				return ImeMode.Inherit;
			}
		}

		/// <summary>Возвращает стандартный режим редактора методов ввода, поддерживаемый данным элементом управления.</summary>
		/// <returns>Одно из значений <see cref="T:System.Windows.Forms.ImeMode" />.</returns>
		protected virtual ImeMode DefaultImeMode
		{
			get
			{
				return ImeMode.Inherit;
			}
		}

		internal int DisableImeModeChangedCount
		{
			get
			{
				bool flag;
				return this.Properties.GetInteger(Control.PropDisableImeModeChangedCount, out flag);
			}
			set
			{
				this.Properties.SetInteger(Control.PropDisableImeModeChangedCount, value);
			}
		}

		private static bool IgnoreWmImeNotify
		{
			get
			{
				return Control.ignoreWmImeNotify;
			}
			set
			{
				Control.ignoreWmImeNotify = value;
			}
		}

		/// <summary>Возвращает или задает режим редактора метода ввода элемента управления.</summary>
		/// <returns>Одно из значений <see cref="T:System.Windows.Forms.ImeMode" />.Значением по умолчанию является <see cref="F:System.Windows.Forms.ImeMode.Inherit" />.</returns>
		/// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">Назначенное значение не является значением перечисления <see cref="T:System.Windows.Forms.ImeMode" />. </exception>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[AmbientValue(ImeMode.Inherit), Localizable(true), SRCategory("CatBehavior"), SRDescription("ControlIMEModeDescr")]
		public ImeMode ImeMode
		{
			get
			{
				ImeMode imeMode = this.ImeModeBase;
				if (imeMode == ImeMode.OnHalf)
				{
					imeMode = ImeMode.On;
				}
				return imeMode;
			}
			set
			{
				this.ImeModeBase = value;
			}
		}

		/// <summary>Получает или задает режим IME элемента управления.</summary>
		/// <returns>Режим IME элемента управления.</returns>
		protected virtual ImeMode ImeModeBase
		{
			get
			{
				return this.CachedImeMode;
			}
			set
			{
				if (!ClientUtils.IsEnumValid(value, (int)value, -1, 12))
				{
					throw new InvalidEnumArgumentException("ImeMode", (int)value, typeof(ImeMode));
				}
				ImeMode cachedImeMode = this.CachedImeMode;
				this.CachedImeMode = value;
				if (cachedImeMode != value)
				{
					Control control = null;
					if (!base.DesignMode && ImeModeConversion.InputLanguageTable != ImeModeConversion.UnsupportedTable)
					{
						if (this.Focused)
						{
							control = this;
						}
						else if (this.ContainsFocus)
						{
							control = Control.FromChildHandleInternal(UnsafeNativeMethods.GetFocus());
						}
						if (control != null && control.CanEnableIme)
						{
							int disableImeModeChangedCount = this.DisableImeModeChangedCount;
							this.DisableImeModeChangedCount = disableImeModeChangedCount + 1;
							try
							{
								control.UpdateImeContextMode();
							}
							finally
							{
								disableImeModeChangedCount = this.DisableImeModeChangedCount;
								this.DisableImeModeChangedCount = disableImeModeChangedCount - 1;
							}
						}
					}
					this.VerifyImeModeChanged(cachedImeMode, this.CachedImeMode);
				}
			}
		}

		private bool ImeSupported
		{
			get
			{
				return this.DefaultImeMode != ImeMode.Disable;
			}
		}

		internal int ImeWmCharsToIgnore
		{
			get
			{
				return this.Properties.GetInteger(Control.PropImeWmCharsToIgnore);
			}
			set
			{
				if (this.ImeWmCharsToIgnore != -1)
				{
					this.Properties.SetInteger(Control.PropImeWmCharsToIgnore, value);
				}
			}
		}

		private bool LastCanEnableIme
		{
			get
			{
				bool flag;
				int integer = this.Properties.GetInteger(Control.PropLastCanEnableIme, out flag);
				flag = (!flag || integer == 1);
				return flag;
			}
			set
			{
				this.Properties.SetInteger(Control.PropLastCanEnableIme, value ? 1 : 0);
			}
		}

		/// <summary>Получает объект, представляющий режим IME распространения.</summary>
		/// <returns>Объект, представляющий режим IME распространения.</returns>
		protected static ImeMode PropagatingImeMode
		{
			get
			{
				if (Control.propagatingImeMode == ImeMode.Inherit)
				{
					ImeMode imeMode = ImeMode.Inherit;
					IntPtr intPtr = UnsafeNativeMethods.GetFocus();
					if (intPtr != IntPtr.Zero)
					{
						imeMode = ImeContext.GetImeMode(intPtr);
						if (imeMode == ImeMode.Disable)
						{
							intPtr = UnsafeNativeMethods.GetAncestor(new HandleRef(null, intPtr), 2);
							if (intPtr != IntPtr.Zero)
							{
								imeMode = ImeContext.GetImeMode(intPtr);
							}
						}
					}
					Control.PropagatingImeMode = imeMode;
				}
				return Control.propagatingImeMode;
			}
			private set
			{
				if (Control.propagatingImeMode != value)
				{
					if (value == ImeMode.NoControl || value == ImeMode.Disable)
					{
						return;
					}
					Control.propagatingImeMode = value;
				}
			}
		}

		static Control()
		{
			Control.EventAutoSizeChanged = new object();
			Control.EventKeyDown = new object();
			Control.EventKeyPress = new object();
			Control.EventKeyUp = new object();
			Control.EventMouseDown = new object();
			Control.EventMouseEnter = new object();
			Control.EventMouseLeave = new object();
			Control.EventMouseHover = new object();
			Control.EventMouseMove = new object();
			Control.EventMouseUp = new object();
			Control.EventMouseWheel = new object();
			Control.EventClick = new object();
			Control.EventClientSize = new object();
			Control.EventDoubleClick = new object();
			Control.EventMouseClick = new object();
			Control.EventMouseDoubleClick = new object();
			Control.EventMouseCaptureChanged = new object();
			Control.EventMove = new object();
			Control.EventResize = new object();
			Control.EventLayout = new object();
			Control.EventGotFocus = new object();
			Control.EventLostFocus = new object();
			Control.EventEnabledChanged = new object();
			Control.EventEnter = new object();
			Control.EventLeave = new object();
			Control.EventHandleCreated = new object();
			Control.EventHandleDestroyed = new object();
			Control.EventVisibleChanged = new object();
			Control.EventControlAdded = new object();
			Control.EventControlRemoved = new object();
			Control.EventChangeUICues = new object();
			Control.EventSystemColorsChanged = new object();
			Control.EventValidating = new object();
			Control.EventValidated = new object();
			Control.EventStyleChanged = new object();
			Control.EventImeModeChanged = new object();
			Control.EventHelpRequested = new object();
			Control.EventPaint = new object();
			Control.EventInvalidated = new object();
			Control.EventQueryContinueDrag = new object();
			Control.EventGiveFeedback = new object();
			Control.EventDragEnter = new object();
			Control.EventDragLeave = new object();
			Control.EventDragOver = new object();
			Control.EventDragDrop = new object();
			Control.EventQueryAccessibilityHelp = new object();
			Control.EventBackgroundImage = new object();
			Control.EventBackgroundImageLayout = new object();
			Control.EventBindingContext = new object();
			Control.EventBackColor = new object();
			Control.EventParent = new object();
			Control.EventVisible = new object();
			Control.EventText = new object();
			Control.EventTabStop = new object();
			Control.EventTabIndex = new object();
			Control.EventSize = new object();
			Control.EventRightToLeft = new object();
			Control.EventLocation = new object();
			Control.EventForeColor = new object();
			Control.EventFont = new object();
			Control.EventEnabled = new object();
			Control.EventDock = new object();
			Control.EventCursor = new object();
			Control.EventContextMenu = new object();
			Control.EventContextMenuStrip = new object();
			Control.EventCausesValidation = new object();
			Control.EventRegionChanged = new object();
			Control.EventMarginChanged = new object();
			Control.EventPaddingChanged = new object();
			Control.EventPreviewKeyDown = new object();
			Control.mouseWheelMessage = 522;
			Control.checkForIllegalCrossThreadCalls = Debugger.IsAttached;
			Control.inCrossThreadSafeCall = false;
			Control.currentHelpInfo = null;
			Control.PropName = PropertyStore.CreateKey();
			Control.PropBackBrush = PropertyStore.CreateKey();
			Control.PropFontHeight = PropertyStore.CreateKey();
			Control.PropCurrentAmbientFont = PropertyStore.CreateKey();
			Control.PropControlsCollection = PropertyStore.CreateKey();
			Control.PropBackColor = PropertyStore.CreateKey();
			Control.PropForeColor = PropertyStore.CreateKey();
			Control.PropFont = PropertyStore.CreateKey();
			Control.PropBackgroundImage = PropertyStore.CreateKey();
			Control.PropFontHandleWrapper = PropertyStore.CreateKey();
			Control.PropUserData = PropertyStore.CreateKey();
			Control.PropContextMenu = PropertyStore.CreateKey();
			Control.PropCursor = PropertyStore.CreateKey();
			Control.PropRegion = PropertyStore.CreateKey();
			Control.PropRightToLeft = PropertyStore.CreateKey();
			Control.PropBindings = PropertyStore.CreateKey();
			Control.PropBindingManager = PropertyStore.CreateKey();
			Control.PropAccessibleDefaultActionDescription = PropertyStore.CreateKey();
			Control.PropAccessibleDescription = PropertyStore.CreateKey();
			Control.PropAccessibility = PropertyStore.CreateKey();
			Control.PropNcAccessibility = PropertyStore.CreateKey();
			Control.PropAccessibleName = PropertyStore.CreateKey();
			Control.PropAccessibleRole = PropertyStore.CreateKey();
			Control.PropPaintingException = PropertyStore.CreateKey();
			Control.PropActiveXImpl = PropertyStore.CreateKey();
			Control.PropControlVersionInfo = PropertyStore.CreateKey();
			Control.PropBackgroundImageLayout = PropertyStore.CreateKey();
			Control.PropAccessibleHelpProvider = PropertyStore.CreateKey();
			Control.PropContextMenuStrip = PropertyStore.CreateKey();
			Control.PropAutoScrollOffset = PropertyStore.CreateKey();
			Control.PropUseCompatibleTextRendering = PropertyStore.CreateKey();
			Control.PropImeWmCharsToIgnore = PropertyStore.CreateKey();
			Control.PropImeMode = PropertyStore.CreateKey();
			Control.PropDisableImeModeChangedCount = PropertyStore.CreateKey();
			Control.PropLastCanEnableIme = PropertyStore.CreateKey();
			Control.PropCacheTextCount = PropertyStore.CreateKey();
			Control.PropCacheTextField = PropertyStore.CreateKey();
			Control.PropAmbientPropertiesService = PropertyStore.CreateKey();
			Control.UseCompatibleTextRenderingDefault = true;
			Control.propagatingImeMode = ImeMode.Inherit;
			Control.lastLanguageChinese = false;
			Control.WM_GETCONTROLNAME = SafeNativeMethods.RegisterWindowMessage("WM_GETCONTROLNAME");
			Control.WM_GETCONTROLTYPE = SafeNativeMethods.RegisterWindowMessage("WM_GETCONTROLTYPE");
		}

		/// <summary>Инициализирует новый экземпляр класса <see cref="T:System.Windows.Forms.Control" />, используя значения параметров по умолчанию.</summary>
		public Control() : this(true)
		{
		}

		internal Control(bool autoInstallSyncContext)
		{
			this.propertyStore = new PropertyStore();
			this.window = new Control.ControlNativeWindow(this);
			this.RequiredScalingEnabled = true;
			this.RequiredScaling = BoundsSpecified.All;
			this.tabIndex = -1;
			this.state = 131086;
			this.state2 = 8;
			this.SetStyle(ControlStyles.UserPaint | ControlStyles.StandardClick | ControlStyles.Selectable | ControlStyles.StandardDoubleClick | ControlStyles.AllPaintingInWmPaint | ControlStyles.UseTextForAccessibility, true);
			this.InitMouseWheelSupport();
			if (this.DefaultMargin != CommonProperties.DefaultMargin)
			{
				this.Margin = this.DefaultMargin;
			}
			if (this.DefaultMinimumSize != CommonProperties.DefaultMinimumSize)
			{
				this.MinimumSize = this.DefaultMinimumSize;
			}
			if (this.DefaultMaximumSize != CommonProperties.DefaultMaximumSize)
			{
				this.MaximumSize = this.DefaultMaximumSize;
			}
			Size defaultSize = this.DefaultSize;
			this.width = defaultSize.Width;
			this.height = defaultSize.Height;
			CommonProperties.xClearPreferredSizeCache(this);
			if (this.width != 0 && this.height != 0)
			{
				NativeMethods.RECT rECT = default(NativeMethods.RECT);
				rECT.left = (rECT.right = (rECT.top = (rECT.bottom = 0)));
				CreateParams createParams = this.CreateParams;
				SafeNativeMethods.AdjustWindowRectEx(ref rECT, createParams.Style, false, createParams.ExStyle);
				this.clientWidth = this.width - (rECT.right - rECT.left);
				this.clientHeight = this.height - (rECT.bottom - rECT.top);
			}
			if (autoInstallSyncContext)
			{
				WindowsFormsSynchronizationContext.InstallIfNeeded();
			}
		}

		/// <summary>Инициализирует новый экземпляр класса <see cref="T:System.Windows.Forms.Control" /> с конкретным текстом.</summary>
		/// <param name="text">Текст, отображаемый элементом управления. </param>
		public Control(string text) : this(null, text)
		{
		}

		/// <summary>Инициализирует новый экземпляр класса <see cref="T:System.Windows.Forms.Control" /> с конкретным текстом, размером и местоположением.</summary>
		/// <param name="text">Текст, отображаемый элементом управления. </param>
		/// <param name="left">Позиция <see cref="P:System.Drawing.Point.X" /> элемента управления относительно левого края контейнера элемента управления (в точках).Значение присваивается свойству <see cref="P:System.Windows.Forms.Control.Left" />.</param>
		/// <param name="top">Позиция <see cref="P:System.Drawing.Point.Y" /> элемента управления относительно верхнего края контейнера элемента управления (в точках).Значение присваивается свойству <see cref="P:System.Windows.Forms.Control.Top" />.</param>
		/// <param name="width">Ширина элемента управления (в точках).Значение, назначенное свойству <see cref="P:System.Windows.Forms.Control.Width" />.</param>
		/// <param name="height">Высота элемента управления (в точках).Значение, назначенное свойству <see cref="P:System.Windows.Forms.Control.Height" />.</param>
		public Control(string text, int left, int top, int width, int height) : this(null, text, left, top, width, height)
		{
		}

		/// <summary>Инициализирует новый экземпляр класса <see cref="T:System.Windows.Forms.Control" /> как дочерний элемент управления с конкретным текстом.</summary>
		/// <param name="parent">Объект <see cref="T:System.Windows.Forms.Control" />, который будет родительским по отношению к элементу управления. </param>
		/// <param name="text">Текст, отображаемый элементом управления. </param>
		public Control(Control parent, string text) : this()
		{
			this.Parent = parent;
			this.Text = text;
		}

		/// <summary>Инициализирует новый экземпляр класса <see cref="T:System.Windows.Forms.Control" /> как дочерний элемент управления с определенным текстом, размером и местоположением.</summary>
		/// <param name="parent">Объект <see cref="T:System.Windows.Forms.Control" />, который будет родительским по отношению к элементу управления. </param>
		/// <param name="text">Текст, отображаемый элементом управления. </param>
		/// <param name="left">Позиция <see cref="P:System.Drawing.Point.X" /> элемента управления относительно левого края контейнера элемента управления (в точках).Значение присваивается свойству <see cref="P:System.Windows.Forms.Control.Left" />.</param>
		/// <param name="top">Позиция <see cref="P:System.Drawing.Point.Y" /> элемента управления относительно верхнего края контейнера элемента управления (в точках).Значение присваивается свойству <see cref="P:System.Windows.Forms.Control.Top" />.</param>
		/// <param name="width">Ширина элемента управления (в точках).Значение, назначенное свойству <see cref="P:System.Windows.Forms.Control.Width" />.</param>
		/// <param name="height">Высота элемента управления (в точках).Значение, назначенное свойству <see cref="P:System.Windows.Forms.Control.Height" />.</param>
		public Control(Control parent, string text, int left, int top, int width, int height) : this(parent, text)
		{
			this.Location = new Point(left, top);
			this.Size = new Size(width, height);
		}

		private AccessibleObject GetAccessibilityObject(int accObjId)
		{
			AccessibleObject result;
			if (accObjId != -4)
			{
				if (accObjId != 0)
				{
					if (accObjId > 0)
					{
						result = this.GetAccessibilityObjectById(accObjId);
					}
					else
					{
						result = null;
					}
				}
				else
				{
					result = this.NcAccessibilityObject;
				}
			}
			else
			{
				result = this.AccessibilityObject;
			}
			return result;
		}

		/// <summary>Получает указанный объект <see cref="T:System.Windows.Forms.AccessibleObject" />.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.AccessibleObject" />.</returns>
		/// <param name="objectId">Int32, указывающий получаемый объект <see cref="T:System.Windows.Forms.AccessibleObject" />.</param>
		protected virtual AccessibleObject GetAccessibilityObjectById(int objectId)
		{
			return null;
		}

		/// <summary>Задает значение, указывающее, как будет вести себя элемент управления, когда его свойство <see cref="P:System.Windows.Forms.Control.AutoSize" /> включено.</summary>
		/// <param name="mode">Одно из значений <see cref="T:System.Windows.Forms.AutoSizeMode" />.</param>
		protected void SetAutoSizeMode(AutoSizeMode mode)
		{
			CommonProperties.SetAutoSizeMode(this, mode);
		}

		/// <summary>Получает значение, указывающее, как будет вести себя элемент управления, когда его свойство <see cref="P:System.Windows.Forms.Control.AutoSize" /> включено.</summary>
		/// <returns>Одно из значений <see cref="T:System.Windows.Forms.AutoSizeMode" />. </returns>
		protected AutoSizeMode GetAutoSizeMode()
		{
			return CommonProperties.GetAutoSizeMode(this);
		}

		private bool ShouldSerializeAccessibleName()
		{
			string accessibleName = this.AccessibleName;
			return accessibleName != null && accessibleName.Length > 0;
		}

		/// <summary>Вызывает в элементе управления, привязанном к компоненту <see cref="T:System.Windows.Forms.BindingSource" />, повторное считывание всех элементов списка и обновление их отображаемых значений.</summary>
		/// <filterpriority>2</filterpriority>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void ResetBindings()
		{
			ControlBindingsCollection controlBindingsCollection = (ControlBindingsCollection)this.Properties.GetObject(Control.PropBindings);
			if (controlBindingsCollection != null)
			{
				controlBindingsCollection.Clear();
			}
		}

		internal virtual void NotifyValidationResult(object sender, CancelEventArgs ev)
		{
			this.ValidationCancelled = ev.Cancel;
		}

		internal bool ValidateActiveControl(out bool validatedControlAllowsFocusChange)
		{
			bool result = true;
			validatedControlAllowsFocusChange = false;
			IContainerControl containerControlInternal = this.GetContainerControlInternal();
			if (containerControlInternal != null && this.CausesValidation)
			{
				ContainerControl containerControl = containerControlInternal as ContainerControl;
				if (containerControl != null)
				{
					while (containerControl.ActiveControl == null)
					{
						Control parentInternal = containerControl.ParentInternal;
						if (parentInternal == null)
						{
							break;
						}
						ContainerControl containerControl2 = parentInternal.GetContainerControlInternal() as ContainerControl;
						if (containerControl2 == null)
						{
							break;
						}
						containerControl = containerControl2;
					}
					result = containerControl.ValidateInternal(true, out validatedControlAllowsFocusChange);
				}
			}
			return result;
		}

		private void DetachContextMenu(object sender, EventArgs e)
		{
			this.ContextMenu = null;
		}

		private void DetachContextMenuStrip(object sender, EventArgs e)
		{
			this.ContextMenuStrip = null;
		}

		private void DisposeFontHandle()
		{
			if (this.Properties.ContainsObject(Control.PropFontHandleWrapper))
			{
				Control.FontHandleWrapper fontHandleWrapper = this.Properties.GetObject(Control.PropFontHandleWrapper) as Control.FontHandleWrapper;
				if (fontHandleWrapper != null)
				{
					fontHandleWrapper.Dispose();
				}
				this.Properties.SetObject(Control.PropFontHandleWrapper, null);
			}
		}

		private Font GetParentFont()
		{
			if (this.ParentInternal != null && this.ParentInternal.CanAccessProperties)
			{
				return this.ParentInternal.Font;
			}
			return null;
		}

		/// <summary>Извлекает размер прямоугольной области, в которую помещается элемент управления.</summary>
		/// <returns>Упорядоченная пара значений типа <see cref="T:System.Drawing.Size" />, представляющая ширину и высоту прямоугольника.</returns>
		/// <param name="proposedSize">Область пользовательского размера для элемента управления. </param>
		/// <filterpriority>2</filterpriority>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public virtual Size GetPreferredSize(Size proposedSize)
		{
			Size size;
			if (this.GetState(6144))
			{
				size = CommonProperties.xGetPreferredSizeCache(this);
			}
			else
			{
				proposedSize = LayoutUtils.ConvertZeroToUnbounded(proposedSize);
				proposedSize = this.ApplySizeConstraints(proposedSize);
				if (this.GetState2(2048))
				{
					Size result = CommonProperties.xGetPreferredSizeCache(this);
					if (!result.IsEmpty && proposedSize == LayoutUtils.MaxSize)
					{
						return result;
					}
				}
				this.CacheTextInternal = true;
				try
				{
					size = this.GetPreferredSizeCore(proposedSize);
				}
				finally
				{
					this.CacheTextInternal = false;
				}
				size = this.ApplySizeConstraints(size);
				if (this.GetState2(2048) && proposedSize == LayoutUtils.MaxSize)
				{
					CommonProperties.xSetPreferredSizeCache(this, size);
				}
			}
			return size;
		}

		internal virtual Size GetPreferredSizeCore(Size proposedSize)
		{
			return CommonProperties.GetSpecifiedBounds(this).Size;
		}

		private bool IsValidBackColor(Color c)
		{
			return c.IsEmpty || this.GetStyle(ControlStyles.SupportsTransparentBackColor) || c.A >= 255;
		}

		internal virtual void AddReflectChild()
		{
		}

		internal virtual void RemoveReflectChild()
		{
		}

		private bool RenderColorTransparent(Color c)
		{
			return this.GetStyle(ControlStyles.SupportsTransparentBackColor) && c.A < 255;
		}

		private void WaitForWaitHandle(WaitHandle waitHandle)
		{
			Application.ThreadContext threadContext = Application.ThreadContext.FromId(this.CreateThreadId);
			if (threadContext == null)
			{
				return;
			}
			IntPtr handle = threadContext.GetHandle();
			bool flag = false;
			uint num = 0u;
			while (!flag)
			{
				bool exitCodeThread = UnsafeNativeMethods.GetExitCodeThread(handle, out num);
				if ((exitCodeThread && num != 259u) || AppDomain.CurrentDomain.IsFinalizingForUnload())
				{
					if (!waitHandle.WaitOne(1, false))
					{
						throw new InvalidAsynchronousStateException(SR.GetString("ThreadNoLongerValid"));
					}
					break;
				}
				else
				{
					if (this.IsDisposed && this.threadCallbackList != null && this.threadCallbackList.Count > 0)
					{
						Queue obj = this.threadCallbackList;
						lock (obj)
						{
							Exception exception = new ObjectDisposedException(base.GetType().Name);
							while (this.threadCallbackList.Count > 0)
							{
								Control.ThreadMethodEntry expr_B8 = (Control.ThreadMethodEntry)this.threadCallbackList.Dequeue();
								expr_B8.exception = exception;
								expr_B8.Complete();
							}
						}
					}
					flag = waitHandle.WaitOne(1000, false);
				}
			}
		}

		/// <summary>Уведомляет клиентские приложения со специальными возможностями об указанном перечислении <see cref="T:System.Windows.Forms.AccessibleEvents" /> для указанного дочернего элемента управления.</summary>
		/// <param name="accEvent">Перечисление <see cref="T:System.Windows.Forms.AccessibleEvents" />, о котором требуется уведомлять клиентские приложения со специальными возможностями. </param>
		/// <param name="childID">Дочерний объект <see cref="T:System.Windows.Forms.Control" />, который требуется уведомлять о событии, связанном со специальными возможностями. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected internal void AccessibilityNotifyClients(AccessibleEvents accEvent, int childID)
		{
			this.AccessibilityNotifyClients(accEvent, -4, childID);
		}

		/// <summary>Уведомляет клиентские приложения со специальными возможностями о заданном перечислении <see cref="T:System.Windows.Forms.AccessibleEvents" /> для указанного дочернего элемента управления.</summary>
		/// <param name="accEvent">Перечисление <see cref="T:System.Windows.Forms.AccessibleEvents" />, о котором требуется уведомлять клиентские приложения со специальными возможностями.</param>
		/// <param name="objectID">Идентификатор объекта <see cref="T:System.Windows.Forms.AccessibleObject" />.</param>
		/// <param name="childID">Дочерний объект <see cref="T:System.Windows.Forms.Control" />, который требуется уведомлять о событии, связанном со специальными возможностями.</param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void AccessibilityNotifyClients(AccessibleEvents accEvent, int objectID, int childID)
		{
			if (this.IsHandleCreated)
			{
				UnsafeNativeMethods.NotifyWinEvent((int)accEvent, new HandleRef(this, this.Handle), objectID, childID + 1);
			}
		}

		private IntPtr ActiveXMergeRegion(IntPtr region)
		{
			return this.ActiveXInstance.MergeRegion(region);
		}

		private void ActiveXOnFocus(bool focus)
		{
			this.ActiveXInstance.OnFocus(focus);
		}

		private void ActiveXViewChanged()
		{
			this.ActiveXInstance.ViewChangedInternal();
		}

		private void ActiveXUpdateBounds(ref int x, ref int y, ref int width, ref int height, int flags)
		{
			this.ActiveXInstance.UpdateBounds(ref x, ref y, ref width, ref height, flags);
		}

		internal virtual void AssignParent(Control value)
		{
			if (value != null)
			{
				this.RequiredScalingEnabled = value.RequiredScalingEnabled;
			}
			if (this.CanAccessProperties)
			{
				Font font = this.Font;
				Color foreColor = this.ForeColor;
				Color backColor = this.BackColor;
				RightToLeft rightToLeft = this.RightToLeft;
				bool enabled = this.Enabled;
				bool visible = this.Visible;
				this.parent = value;
				this.OnParentChanged(EventArgs.Empty);
				if (this.GetAnyDisposingInHierarchy())
				{
					return;
				}
				if (enabled != this.Enabled)
				{
					this.OnEnabledChanged(EventArgs.Empty);
				}
				bool visible2 = this.Visible;
				if (visible != visible2 && (!(!visible & visible2) || this.parent != null || this.GetTopLevel()))
				{
					this.OnVisibleChanged(EventArgs.Empty);
				}
				if (!font.Equals(this.Font))
				{
					this.OnFontChanged(EventArgs.Empty);
				}
				if (!foreColor.Equals(this.ForeColor))
				{
					this.OnForeColorChanged(EventArgs.Empty);
				}
				if (!backColor.Equals(this.BackColor))
				{
					this.OnBackColorChanged(EventArgs.Empty);
				}
				if (rightToLeft != this.RightToLeft)
				{
					this.OnRightToLeftChanged(EventArgs.Empty);
				}
				if (this.Properties.GetObject(Control.PropBindingManager) == null && this.Created)
				{
					this.OnBindingContextChanged(EventArgs.Empty);
				}
			}
			else
			{
				this.parent = value;
				this.OnParentChanged(EventArgs.Empty);
			}
			this.SetState(16777216, false);
			if (this.ParentInternal != null)
			{
				this.ParentInternal.LayoutEngine.InitLayout(this, BoundsSpecified.All);
			}
		}

		/// <summary>Выполняет указанный делегат асинхронно в потоке, в котором был создан базовый дескриптор элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.IAsyncResult" />, который представляет результат выполнения операции <see cref="M:System.Windows.Forms.Control.BeginInvoke(System.Delegate)" />.</returns>
		/// <param name="method">Делегат метода, который не принимает параметров. </param>
		/// <exception cref="T:System.InvalidOperationException">Соответствующий дескриптор окна не обнаружен.</exception>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public IAsyncResult BeginInvoke(Delegate method)
		{
			return this.BeginInvoke(method, null);
		}

		/// <summary>Выполняет указанный делегат асинхронно с указанными аргументами в потоке, в котором был создан базовый дескриптор элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.IAsyncResult" />, который представляет результат выполнения операции <see cref="M:System.Windows.Forms.Control.BeginInvoke(System.Delegate)" />.</returns>
		/// <param name="method">Делегат метода, принимающий параметры, количество и тип которых является таким же, что и в параметре <paramref name="args" />. </param>
		/// <param name="args">Массив объектов, передаваемых в качестве аргументов указанному методу.Это может быть значение null, если аргументы не требуются.</param>
		/// <exception cref="T:System.InvalidOperationException">Соответствующий дескриптор окна не обнаружен.</exception>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public IAsyncResult BeginInvoke(Delegate method, params object[] args)
		{
			IAsyncResult result;
			using (new Control.MultithreadSafeCallScope())
			{
				result = (IAsyncResult)this.FindMarshalingControl().MarshaledInvoke(this, method, args, false);
			}
			return result;
		}

		internal void BeginUpdateInternal()
		{
			if (!this.IsHandleCreated)
			{
				return;
			}
			if (this.updateCount == 0)
			{
				this.SendMessage(11, 0, 0);
			}
			this.updateCount += 1;
		}

		/// <summary>Помещает элемент управления в начало z-порядка.</summary>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void BringToFront()
		{
			if (this.parent != null)
			{
				this.parent.Controls.SetChildIndex(this, 0);
				return;
			}
			if (this.IsHandleCreated && this.GetTopLevel() && SafeNativeMethods.IsWindowEnabled(new HandleRef(this.window, this.Handle)))
			{
				SafeNativeMethods.SetWindowPos(new HandleRef(this.window, this.Handle), NativeMethods.HWND_TOP, 0, 0, 0, 0, 3);
			}
		}

		internal virtual bool CanProcessMnemonic()
		{
			return this.Enabled && this.Visible && (this.parent == null || this.parent.CanProcessMnemonic());
		}

		internal virtual bool CanSelectCore()
		{
			if ((this.controlStyle & ControlStyles.Selectable) != ControlStyles.Selectable)
			{
				return false;
			}
			for (Control control = this; control != null; control = control.parent)
			{
				if (!control.Enabled || !control.Visible)
				{
					return false;
				}
			}
			return true;
		}

		internal static void CheckParentingCycle(Control bottom, Control toFind)
		{
			Form form = null;
			Control control = null;
			for (Control control2 = bottom; control2 != null; control2 = control2.ParentInternal)
			{
				control = control2;
				if (control2 == toFind)
				{
					throw new ArgumentException(SR.GetString("CircularOwner"));
				}
			}
			if (control != null && control is Form)
			{
				for (Form form2 = (Form)control; form2 != null; form2 = form2.OwnerInternal)
				{
					form = form2;
					if (form2 == toFind)
					{
						throw new ArgumentException(SR.GetString("CircularOwner"));
					}
				}
			}
			if (form != null && form.ParentInternal != null)
			{
				Control.CheckParentingCycle(form.ParentInternal, toFind);
			}
		}

		private void ChildGotFocus(Control child)
		{
			if (this.IsActiveX)
			{
				this.ActiveXOnFocus(true);
			}
			if (this.parent != null)
			{
				this.parent.ChildGotFocus(child);
			}
		}

		/// <summary>Получает значение, показывающее, является ли указанный элемент управления дочерним элементом.</summary>
		/// <returns>Значение true, если указанный элемент управления является дочерним элементом; в противном случае — значение false.</returns>
		/// <param name="ctl">Оцениваемый объект <see cref="T:System.Windows.Forms.Control" />. </param>
		/// <filterpriority>1</filterpriority>
		public bool Contains(Control ctl)
		{
			while (ctl != null)
			{
				ctl = ctl.ParentInternal;
				if (ctl == null)
				{
					return false;
				}
				if (ctl == this)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Создает для элемента управления новый объект с поддержкой специальных возможностей.</summary>
		/// <returns>Новый объект <see cref="T:System.Windows.Forms.AccessibleObject" /> для элемента управления.</returns>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual AccessibleObject CreateAccessibilityInstance()
		{
			return new Control.ControlAccessibleObject(this);
		}

		/// <summary>Создает новый экземпляр коллекции элементов управления для указанного элемента управления.</summary>
		/// <returns>Новый экземпляр коллекции <see cref="T:System.Windows.Forms.Control.ControlCollection" />, назначенной элементу управления.</returns>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual Control.ControlCollection CreateControlsInstance()
		{
			return new Control.ControlCollection(this);
		}

		/// <summary>Задает объект <see cref="T:System.Drawing.Graphics" /> для элемента управления.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Graphics" /> для элемента управления.</returns>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public Graphics CreateGraphics()
		{
			Graphics result;
			using (new Control.MultithreadSafeCallScope())
			{
				IntSecurity.CreateGraphicsForControl.Demand();
				result = this.CreateGraphicsInternal();
			}
			return result;
		}

		internal Graphics CreateGraphicsInternal()
		{
			return Graphics.FromHwndInternal(this.Handle);
		}

		/// <summary>Создает дескриптор для элемента управления.</summary>
		/// <exception cref="T:System.ObjectDisposedException">Состояние объекта — удален. </exception>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		[UIPermission(SecurityAction.InheritanceDemand, Window = UIPermissionWindow.AllWindows)]
		protected virtual void CreateHandle()
		{
			IntPtr userCookie = IntPtr.Zero;
			if (this.GetState(2048))
			{
				throw new ObjectDisposedException(base.GetType().Name);
			}
			if (this.GetState(262144))
			{
				return;
			}
			Rectangle bounds;
			try
			{
				this.SetState(262144, true);
				bounds = this.Bounds;
				if (Application.UseVisualStyles)
				{
					userCookie = UnsafeNativeMethods.ThemingScope.Activate();
				}
				CreateParams createParams = this.CreateParams;
				this.SetState(1073741824, (createParams.ExStyle & 4194304) != 0);
				if (this.parent != null)
				{
					Rectangle clientRectangle = this.parent.ClientRectangle;
					if (!clientRectangle.IsEmpty)
					{
						if (createParams.X != -2147483648)
						{
							createParams.X -= clientRectangle.X;
						}
						if (createParams.Y != -2147483648)
						{
							createParams.Y -= clientRectangle.Y;
						}
					}
				}
				if (createParams.Parent == IntPtr.Zero && (createParams.Style & 1073741824) != 0)
				{
					Application.ParkHandle(createParams);
				}
				this.window.CreateHandle(createParams);
				this.UpdateReflectParent(true);
			}
			finally
			{
				this.SetState(262144, false);
				UnsafeNativeMethods.ThemingScope.Deactivate(userCookie);
			}
			if (this.Bounds != bounds)
			{
				LayoutTransaction.DoLayout(this.ParentInternal, this, PropertyNames.Bounds);
			}
		}

		/// <summary>Вызывает принудительное создание элемента управления, включая создание дескриптора и любых дочерних элементов.</summary>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void CreateControl()
		{
			bool created = this.Created;
			this.CreateControl(false);
			if (this.Properties.GetObject(Control.PropBindingManager) == null && this.ParentInternal != null && !created)
			{
				this.OnBindingContextChanged(EventArgs.Empty);
			}
		}

		internal void CreateControl(bool fIgnoreVisible)
		{
			if (((this.state & 1) == 0 && this.Visible) | fIgnoreVisible)
			{
				this.state |= 1;
				bool flag = false;
				try
				{
					if (!this.IsHandleCreated)
					{
						this.CreateHandle();
					}
					Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
					if (controlCollection != null)
					{
						Control[] array = new Control[controlCollection.Count];
						controlCollection.CopyTo(array, 0);
						Control[] array2 = array;
						for (int i = 0; i < array2.Length; i++)
						{
							Control control = array2[i];
							if (control.IsHandleCreated)
							{
								control.SetParentHandle(this.Handle);
							}
							control.CreateControl(fIgnoreVisible);
						}
					}
					flag = true;
				}
				finally
				{
					if (!flag)
					{
						this.state &= -2;
					}
				}
				this.OnCreateControl();
			}
		}

		/// <summary>Отправляет заданное сообщение процедуре окна, используемой по умолчанию.</summary>
		/// <param name="m">Класс <see cref="T:System.Windows.Forms.Message" /> Windows для обработки. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode), SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		protected virtual void DefWndProc(ref Message m)
		{
			this.window.DefWndProc(ref m);
		}

		/// <summary>Удаляет дескриптор, сопоставленный с элементом управления.</summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		[UIPermission(SecurityAction.LinkDemand, Window = UIPermissionWindow.AllWindows), SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		protected virtual void DestroyHandle()
		{
			if (this.RecreatingHandle && this.threadCallbackList != null)
			{
				Queue obj = this.threadCallbackList;
				lock (obj)
				{
					if (Control.threadCallbackMessage != 0)
					{
						NativeMethods.MSG mSG = default(NativeMethods.MSG);
						if (UnsafeNativeMethods.PeekMessage(ref mSG, new HandleRef(this, this.Handle), Control.threadCallbackMessage, Control.threadCallbackMessage, 0))
						{
							this.SetState(32768, true);
						}
					}
				}
			}
			if (!this.RecreatingHandle && this.threadCallbackList != null)
			{
				Queue obj = this.threadCallbackList;
				lock (obj)
				{
					Exception exception = new ObjectDisposedException(base.GetType().Name);
					while (this.threadCallbackList.Count > 0)
					{
						Control.ThreadMethodEntry expr_AC = (Control.ThreadMethodEntry)this.threadCallbackList.Dequeue();
						expr_AC.exception = exception;
						expr_AC.Complete();
					}
				}
			}
			if ((64 & (int)((long)UnsafeNativeMethods.GetWindowLong(new HandleRef(this.window, this.InternalHandle), -20))) != 0)
			{
				UnsafeNativeMethods.DefMDIChildProc(this.InternalHandle, 16, IntPtr.Zero, IntPtr.Zero);
			}
			else
			{
				this.window.DestroyHandle();
			}
			this.trackMouseEvent = null;
		}

		/// <summary>Освобождает неуправляемые ресурсы, используемые объектом <see cref="T:System.Windows.Forms.Control" /> и его дочерними элементами управления (при необходимости освобождает и управляемые ресурсы).</summary>
		/// <param name="disposing">Значение true, чтобы освободить все ресурсы (управляемые и неуправляемые); значение false, чтобы освободить только неуправляемые ресурсы. </param>
		protected override void Dispose(bool disposing)
		{
			if (this.GetState(2097152))
			{
				object @object = this.Properties.GetObject(Control.PropBackBrush);
				if (@object != null)
				{
					IntPtr intPtr = (IntPtr)@object;
					if (intPtr != IntPtr.Zero)
					{
						SafeNativeMethods.DeleteObject(new HandleRef(this, intPtr));
					}
					this.Properties.SetObject(Control.PropBackBrush, null);
				}
			}
			this.UpdateReflectParent(false);
			if (disposing)
			{
				if (this.GetState(4096))
				{
					return;
				}
				if (this.GetState(262144))
				{
					throw new InvalidOperationException(SR.GetString("ClosingWhileCreatingHandle", new object[]
					{
						"Dispose"
					}));
				}
				this.SetState(4096, true);
				this.SuspendLayout();
				try
				{
					this.DisposeAxControls();
					ContextMenu contextMenu = (ContextMenu)this.Properties.GetObject(Control.PropContextMenu);
					if (contextMenu != null)
					{
						contextMenu.Disposed -= new EventHandler(this.DetachContextMenu);
					}
					this.ResetBindings();
					if (this.IsHandleCreated)
					{
						this.DestroyHandle();
					}
					if (this.parent != null)
					{
						this.parent.Controls.Remove(this);
					}
					Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
					if (controlCollection != null)
					{
						for (int i = 0; i < controlCollection.Count; i++)
						{
							Control expr_12F = controlCollection[i];
							expr_12F.parent = null;
							expr_12F.Dispose();
						}
						this.Properties.SetObject(Control.PropControlsCollection, null);
					}
					base.Dispose(disposing);
					return;
				}
				finally
				{
					this.ResumeLayout(false);
					this.SetState(4096, false);
					this.SetState(2048, true);
				}
			}
			if (this.window != null)
			{
				this.window.ForceExitMessageLoop();
			}
			base.Dispose(disposing);
		}

		internal virtual void DisposeAxControls()
		{
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			if (controlCollection != null)
			{
				for (int i = 0; i < controlCollection.Count; i++)
				{
					controlCollection[i].DisposeAxControls();
				}
			}
		}

		/// <summary>Начинает операцию перетаскивания.</summary>
		/// <returns>Значение перечисления <see cref="T:System.Windows.Forms.DragDropEffects" />, представляющее конечный результат выполнения операции перетаскивания.</returns>
		/// <param name="data">Перетаскиваемые данные. </param>
		/// <param name="allowedEffects">Одно из значений <see cref="T:System.Windows.Forms.DragDropEffects" />. </param>
		/// <filterpriority>1</filterpriority>
		[UIPermission(SecurityAction.Demand, Clipboard = UIPermissionClipboard.OwnClipboard)]
		public DragDropEffects DoDragDrop(object data, DragDropEffects allowedEffects)
		{
			int[] array = new int[1];
			UnsafeNativeMethods.IOleDropSource dropSource = new DropSource(this);
			System.Runtime.InteropServices.ComTypes.IDataObject dataObject;
			if (data is System.Runtime.InteropServices.ComTypes.IDataObject)
			{
				dataObject = (System.Runtime.InteropServices.ComTypes.IDataObject)data;
			}
			else
			{
				DataObject dataObject2;
				if (data is IDataObject)
				{
					dataObject2 = new DataObject((IDataObject)data);
				}
				else
				{
					dataObject2 = new DataObject();
					dataObject2.SetData(data);
				}
				dataObject = dataObject2;
			}
			try
			{
				SafeNativeMethods.DoDragDrop(dataObject, dropSource, (int)allowedEffects, array);
			}
			catch (Exception arg_55_0)
			{
				if (ClientUtils.IsSecurityOrCriticalException(arg_55_0))
				{
					throw;
				}
			}
			return (DragDropEffects)array[0];
		}

		/// <summary>Поддерживает отрисовку в указанном точечном рисунке.</summary>
		/// <param name="bitmap">Точечный рисунок, который требуется нарисовать.</param>
		/// <param name="targetBounds">Границы, в которых выполняется визуализация элемента управления.</param>
		[UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.AllWindows)]
		public void DrawToBitmap(Bitmap bitmap, Rectangle targetBounds)
		{
			if (bitmap == null)
			{
				throw new ArgumentNullException("bitmap");
			}
			if (targetBounds.Width <= 0 || targetBounds.Height <= 0 || targetBounds.X < 0 || targetBounds.Y < 0)
			{
				throw new ArgumentException("targetBounds");
			}
			if (!this.IsHandleCreated)
			{
				this.CreateHandle();
			}
			int nWidth = Math.Min(this.Width, targetBounds.Width);
			int nHeight = Math.Min(this.Height, targetBounds.Height);
			using (Graphics graphics = Graphics.FromImage(new Bitmap(nWidth, nHeight, bitmap.PixelFormat)))
			{
				IntPtr hdc = graphics.GetHdc();
				UnsafeNativeMethods.SendMessage(new HandleRef(this, this.Handle), 791, hdc, (IntPtr)30);
				using (Graphics graphics2 = Graphics.FromImage(bitmap))
				{
					IntPtr hdc2 = graphics2.GetHdc();
					SafeNativeMethods.BitBlt(new HandleRef(graphics2, hdc2), targetBounds.X, targetBounds.Y, nWidth, nHeight, new HandleRef(graphics, hdc), 0, 0, 13369376);
					graphics2.ReleaseHdcInternal(hdc2);
				}
				graphics.ReleaseHdcInternal(hdc);
			}
		}

		/// <summary>Получает возвращаемое значение асинхронной операции, представленное переданным объектом <see cref="T:System.IAsyncResult" />.</summary>
		/// <returns>Объект <see cref="T:System.Object" />, созданный асинхронной операцией.</returns>
		/// <param name="asyncResult">Объект <see cref="T:System.IAsyncResult" />, представляющий конкретную асинхронную операцию вызова, возвращаемую при вызове <see cref="M:System.Windows.Forms.Control.BeginInvoke(System.Delegate)" />. </param>
		/// <exception cref="T:System.ArgumentNullException">Параметр <paramref name="asyncResult" /> имеет значение null. </exception>
		/// <exception cref="T:System.ArgumentException">Объект <paramref name="asyncResult" /> не был создан предыдущим вызовом метода <see cref="M:System.Windows.Forms.Control.BeginInvoke(System.Delegate)" /> из того же элемента управления. </exception>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public object EndInvoke(IAsyncResult asyncResult)
		{
			object retVal;
			using (new Control.MultithreadSafeCallScope())
			{
				if (asyncResult == null)
				{
					throw new ArgumentNullException("asyncResult");
				}
				Control.ThreadMethodEntry threadMethodEntry = asyncResult as Control.ThreadMethodEntry;
				if (threadMethodEntry == null)
				{
					throw new ArgumentException(SR.GetString("ControlBadAsyncResult"), "asyncResult");
				}
				if (!asyncResult.IsCompleted)
				{
					Control control = this.FindMarshalingControl();
					int num;
					if (SafeNativeMethods.GetWindowThreadProcessId(new HandleRef(control, control.Handle), out num) == SafeNativeMethods.GetCurrentThreadId())
					{
						control.InvokeMarshaledCallbacks();
					}
					else
					{
						control = threadMethodEntry.marshaler;
						control.WaitForWaitHandle(asyncResult.AsyncWaitHandle);
					}
				}
				if (threadMethodEntry.exception != null)
				{
					throw threadMethodEntry.exception;
				}
				retVal = threadMethodEntry.retVal;
			}
			return retVal;
		}

		internal bool EndUpdateInternal()
		{
			return this.EndUpdateInternal(true);
		}

		internal bool EndUpdateInternal(bool invalidate)
		{
			if (this.updateCount > 0)
			{
				this.updateCount -= 1;
				if (this.updateCount == 0)
				{
					this.SendMessage(11, -1, 0);
					if (invalidate)
					{
						this.Invalidate();
					}
				}
				return true;
			}
			return false;
		}

		/// <summary>Получает форму, в которой находится элемент управления.</summary>
		/// <returns>Форма <see cref="T:System.Windows.Forms.Form" />, в которой находится элемент управления.</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.UIPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Window="AllWindows" />
		/// </PermissionSet>
		[UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.AllWindows)]
		public Form FindForm()
		{
			return this.FindFormInternal();
		}

		internal Form FindFormInternal()
		{
			Control control = this;
			while (control != null && !(control is Form))
			{
				control = control.ParentInternal;
			}
			return (Form)control;
		}

		private Control FindMarshalingControl()
		{
			Control result;
			lock (this)
			{
				Control control = this;
				while (control != null && !control.IsHandleCreated)
				{
					control = control.ParentInternal;
				}
				if (control == null)
				{
					control = this;
				}
				result = control;
			}
			return result;
		}

		/// <summary>Определяет, является ли элемент управления элементом верхнего уровня.</summary>
		/// <returns>Значение true, если элемент управления является элементом верхнего уровня; в противном случае — значение false.</returns>
		protected bool GetTopLevel()
		{
			return (this.state & 524288) != 0;
		}

		internal void RaiseCreateHandleEvent(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventHandleCreated];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает соответствующее событие клавиши.</summary>
		/// <param name="key">Вызываемое событие. </param>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.KeyEventArgs" />, содержащий данные, относящиеся к событию. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void RaiseKeyEvent(object key, KeyEventArgs e)
		{
			KeyEventHandler keyEventHandler = (KeyEventHandler)base.Events[key];
			if (keyEventHandler != null)
			{
				keyEventHandler(this, e);
			}
		}

		/// <summary>Вызывает соответствующее событие мыши.</summary>
		/// <param name="key">Вызываемое событие. </param>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.MouseEventArgs" />, содержащий данные, относящиеся к событию. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void RaiseMouseEvent(object key, MouseEventArgs e)
		{
			MouseEventHandler mouseEventHandler = (MouseEventHandler)base.Events[key];
			if (mouseEventHandler != null)
			{
				mouseEventHandler(this, e);
			}
		}

		/// <summary>Задает фокус ввода элемента управления.</summary>
		/// <returns>Значение true, если запрос фокуса ввода был успешным; в противном случае — значение false.</returns>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public bool Focus()
		{
			IntSecurity.ModifyFocus.Demand();
			return this.FocusInternal();
		}

		internal virtual bool FocusInternal()
		{
			if (this.CanFocus)
			{
				UnsafeNativeMethods.SetFocus(new HandleRef(this, this.Handle));
			}
			if (this.Focused && this.ParentInternal != null)
			{
				IContainerControl containerControlInternal = this.ParentInternal.GetContainerControlInternal();
				if (containerControlInternal != null)
				{
					if (containerControlInternal is ContainerControl)
					{
						((ContainerControl)containerControlInternal).SetActiveControlInternal(this);
					}
					else
					{
						containerControlInternal.ActiveControl = this;
					}
				}
			}
			return this.Focused;
		}

		/// <summary>Получает элемент управления, содержащий указанный дескриптор.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.Control" /> представляет элемент управления, сопоставленный с указанным дескриптором; возвращает значение null, если элемент управления с указанным дескриптором не обнаружен.</returns>
		/// <param name="handle">Дескриптор окна (HWND), который требуется найти. </param>
		/// <filterpriority>2</filterpriority>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public static Control FromChildHandle(IntPtr handle)
		{
			IntSecurity.ControlFromHandleOrLocation.Demand();
			return Control.FromChildHandleInternal(handle);
		}

		internal static Control FromChildHandleInternal(IntPtr handle)
		{
			while (handle != IntPtr.Zero)
			{
				Control control = Control.FromHandleInternal(handle);
				if (control != null)
				{
					return control;
				}
				handle = UnsafeNativeMethods.GetAncestor(new HandleRef(null, handle), 1);
			}
			return null;
		}

		/// <summary>Возвращает элемент управления, сопоставленный в данный момент с указанным дескриптором.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.Control" /> представляет элемент управления, сопоставленный с указанным дескриптором; возвращает значение null, если элемент управления с указанным дескриптором не обнаружен.</returns>
		/// <param name="handle">Дескриптор окна (HWND), который требуется найти. </param>
		/// <filterpriority>2</filterpriority>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public static Control FromHandle(IntPtr handle)
		{
			IntSecurity.ControlFromHandleOrLocation.Demand();
			return Control.FromHandleInternal(handle);
		}

		internal static Control FromHandleInternal(IntPtr handle)
		{
			NativeWindow nativeWindow = NativeWindow.FromHandle(handle);
			while (nativeWindow != null && !(nativeWindow is Control.ControlNativeWindow))
			{
				nativeWindow = nativeWindow.PreviousWindow;
			}
			if (nativeWindow is Control.ControlNativeWindow)
			{
				return ((Control.ControlNativeWindow)nativeWindow).GetControl();
			}
			return null;
		}

		internal Size ApplySizeConstraints(int width, int height)
		{
			return this.ApplyBoundsConstraints(0, 0, width, height).Size;
		}

		internal Size ApplySizeConstraints(Size proposedSize)
		{
			return this.ApplyBoundsConstraints(0, 0, proposedSize.Width, proposedSize.Height).Size;
		}

		internal virtual Rectangle ApplyBoundsConstraints(int suggestedX, int suggestedY, int proposedWidth, int proposedHeight)
		{
			if (this.MaximumSize != Size.Empty || this.MinimumSize != Size.Empty)
			{
				Size b = LayoutUtils.ConvertZeroToUnbounded(this.MaximumSize);
				Rectangle result = new Rectangle(suggestedX, suggestedY, 0, 0);
				result.Size = LayoutUtils.IntersectSizes(new Size(proposedWidth, proposedHeight), b);
				result.Size = LayoutUtils.UnionSizes(result.Size, this.MinimumSize);
				return result;
			}
			return new Rectangle(suggestedX, suggestedY, proposedWidth, proposedHeight);
		}

		/// <summary>Получает дочерний элемент управления, расположенный по указанным координатам, определяя, следует ли игнорировать дочерние элементы управления конкретного типа.</summary>
		/// <returns>Дочерний объект <see cref="T:System.Windows.Forms.Control" />, расположенный по указанным координатам.</returns>
		/// <param name="pt">Объект <see cref="T:System.Drawing.Point" />, содержащий координаты, по которым будет проходить поиск элемента управления.Координаты заданы относительно левого верхнего угла клиентской области элемента управления.</param>
		/// <param name="skipValue">Одно из значений объекта <see cref="T:System.Windows.Forms.GetChildAtPointSkip" />, определяющее, следует ли игнорировать дочерние элементы управления конкретного типа.</param>
		public Control GetChildAtPoint(Point pt, GetChildAtPointSkip skipValue)
		{
			if (skipValue < GetChildAtPointSkip.None || skipValue > (GetChildAtPointSkip.Invisible | GetChildAtPointSkip.Disabled | GetChildAtPointSkip.Transparent))
			{
				throw new InvalidEnumArgumentException("skipValue", (int)skipValue, typeof(GetChildAtPointSkip));
			}
			Control control = Control.FromChildHandleInternal(UnsafeNativeMethods.ChildWindowFromPointEx(new HandleRef(null, this.Handle), pt.X, pt.Y, (int)skipValue));
			if (control != null && !this.IsDescendant(control))
			{
				IntSecurity.ControlFromHandleOrLocation.Demand();
			}
			if (control != this)
			{
				return control;
			}
			return null;
		}

		/// <summary>Получает дочерний элемент управления, имеющий указанные координаты.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.Control" /> предоставляет элемент управления, расположенный в указанном месте.</returns>
		/// <param name="pt">Объект <see cref="T:System.Drawing.Point" />, содержащий координаты, по которым будет проходить поиск элемента управления.Координаты заданы относительно левого верхнего угла клиентской области элемента управления.</param>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public Control GetChildAtPoint(Point pt)
		{
			return this.GetChildAtPoint(pt, GetChildAtPointSkip.None);
		}

		/// <summary>Возвращает следующий объект <see cref="T:System.Windows.Forms.ContainerControl" /> в цепочке родительских элементов управления данного элемента.</summary>
		/// <returns>Объект <see cref="T:System.Windows.Forms.IContainerControl" />, представляющий родительский элемент объекта <see cref="T:System.Windows.Forms.Control" />.</returns>
		/// <filterpriority>1</filterpriority>
		public IContainerControl GetContainerControl()
		{
			IntSecurity.GetParent.Demand();
			return this.GetContainerControlInternal();
		}

		private static bool IsFocusManagingContainerControl(Control ctl)
		{
			return (ctl.controlStyle & ControlStyles.ContainerControl) == ControlStyles.ContainerControl && ctl is IContainerControl;
		}

		internal bool IsUpdating()
		{
			return this.updateCount > 0;
		}

		internal IContainerControl GetContainerControlInternal()
		{
			Control control = this;
			if (control != null && this.IsContainerControl)
			{
				control = control.ParentInternal;
			}
			while (control != null && !Control.IsFocusManagingContainerControl(control))
			{
				control = control.ParentInternal;
			}
			return (IContainerControl)control;
		}

		private static Control.FontHandleWrapper GetDefaultFontHandleWrapper()
		{
			if (Control.defaultFontHandleWrapper == null)
			{
				Control.defaultFontHandleWrapper = new Control.FontHandleWrapper(Control.DefaultFont);
			}
			return Control.defaultFontHandleWrapper;
		}

		internal IntPtr GetHRgn(Region region)
		{
			Graphics graphics = this.CreateGraphicsInternal();
			IntPtr expr_0E = region.GetHrgn(graphics);
			System.Internal.HandleCollector.Add(expr_0E, NativeMethods.CommonHandles.GDI);
			graphics.Dispose();
			return expr_0E;
		}

		/// <summary>Получает границы, внутри которых масштабируется элемент управления.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Rectangle" />, представляющий границы, внутри которых масштабируется элемент управления.</returns>
		/// <param name="bounds">Объект <see cref="T:System.Drawing.Rectangle" />, определяющий область, для которой возвращаются границы области отображения.</param>
		/// <param name="factor">Высота и ширина границ элемента управления.</param>
		/// <param name="specified">Одно из значений объекта <see cref="T:System.Windows.Forms.BoundsSpecified" />, задающее границы элемента управления, используемые для определения его размеров и положения.</param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual Rectangle GetScaledBounds(Rectangle bounds, SizeF factor, BoundsSpecified specified)
		{
			NativeMethods.RECT rECT = new NativeMethods.RECT(0, 0, 0, 0);
			CreateParams createParams = this.CreateParams;
			SafeNativeMethods.AdjustWindowRectEx(ref rECT, createParams.Style, this.HasMenu, createParams.ExStyle);
			float num = factor.Width;
			float num2 = factor.Height;
			int num3 = bounds.X;
			int num4 = bounds.Y;
			bool flag = !this.GetState(524288);
			if (flag)
			{
				ISite site = this.Site;
				if (site != null && site.DesignMode)
				{
					IDesignerHost designerHost = site.GetService(typeof(IDesignerHost)) as IDesignerHost;
					if (designerHost != null && designerHost.RootComponent == this)
					{
						flag = false;
					}
				}
			}
			if (flag)
			{
				if ((specified & BoundsSpecified.X) != BoundsSpecified.None)
				{
					num3 = (int)Math.Round((double)((float)bounds.X * num));
				}
				if ((specified & BoundsSpecified.Y) != BoundsSpecified.None)
				{
					num4 = (int)Math.Round((double)((float)bounds.Y * num2));
				}
			}
			int num5 = bounds.Width;
			int num6 = bounds.Height;
			if ((this.controlStyle & ControlStyles.FixedWidth) != ControlStyles.FixedWidth && (specified & BoundsSpecified.Width) != BoundsSpecified.None)
			{
				int num7 = rECT.right - rECT.left;
				num5 = (int)Math.Round((double)((float)(bounds.Width - num7) * num)) + num7;
			}
			if ((this.controlStyle & ControlStyles.FixedHeight) != ControlStyles.FixedHeight && (specified & BoundsSpecified.Height) != BoundsSpecified.None)
			{
				int num8 = rECT.bottom - rECT.top;
				num6 = (int)Math.Round((double)((float)(bounds.Height - num8) * num2)) + num8;
			}
			return new Rectangle(num3, num4, num5, num6);
		}

		private MouseButtons GetXButton(int wparam)
		{
			if (wparam == 1)
			{
				return MouseButtons.XButton1;
			}
			if (wparam != 2)
			{
				return MouseButtons.None;
			}
			return MouseButtons.XButton2;
		}

		internal virtual bool GetVisibleCore()
		{
			return this.GetState(2) && (this.ParentInternal == null || this.ParentInternal.GetVisibleCore());
		}

		internal bool GetAnyDisposingInHierarchy()
		{
			Control control = this;
			bool result = false;
			while (control != null)
			{
				if (control.Disposing)
				{
					result = true;
					break;
				}
				control = control.parent;
			}
			return result;
		}

		private MenuItem GetMenuItemFromHandleId(IntPtr hmenu, int item)
		{
			MenuItem result = null;
			int menuItemID = UnsafeNativeMethods.GetMenuItemID(new HandleRef(null, hmenu), item);
			if (menuItemID == -1)
			{
				IntPtr intPtr = IntPtr.Zero;
				intPtr = UnsafeNativeMethods.GetSubMenu(new HandleRef(null, hmenu), item);
				int menuItemCount = UnsafeNativeMethods.GetMenuItemCount(new HandleRef(null, intPtr));
				MenuItem menuItem = null;
				for (int i = 0; i < menuItemCount; i++)
				{
					menuItem = this.GetMenuItemFromHandleId(intPtr, i);
					if (menuItem != null)
					{
						Menu menu = menuItem.Parent;
						if (menu != null && menu is MenuItem)
						{
							menuItem = (MenuItem)menu;
							break;
						}
						menuItem = null;
					}
				}
				result = menuItem;
			}
			else
			{
				Command commandFromID = Command.GetCommandFromID(menuItemID);
				if (commandFromID != null)
				{
					object target = commandFromID.Target;
					if (target != null && target is MenuItem.MenuItemData)
					{
						result = ((MenuItem.MenuItemData)target).baseItem;
					}
				}
			}
			return result;
		}

		private ArrayList GetChildControlsTabOrderList(bool handleCreatedOnly)
		{
			ArrayList arrayList = new ArrayList();
			foreach (Control control in this.Controls)
			{
				if (!handleCreatedOnly || control.IsHandleCreated)
				{
					ArrayList expr_2C = arrayList;
					expr_2C.Add(new Control.ControlTabOrderHolder(expr_2C.Count, control.TabIndex, control));
				}
			}
			arrayList.Sort(new Control.ControlTabOrderComparer());
			return arrayList;
		}

		private int[] GetChildWindowsInTabOrder()
		{
			ArrayList childWindowsTabOrderList = this.GetChildWindowsTabOrderList();
			int[] array = new int[childWindowsTabOrderList.Count];
			for (int i = 0; i < childWindowsTabOrderList.Count; i++)
			{
				array[i] = ((Control.ControlTabOrderHolder)childWindowsTabOrderList[i]).oldOrder;
			}
			return array;
		}

		internal Control[] GetChildControlsInTabOrder(bool handleCreatedOnly)
		{
			ArrayList childControlsTabOrderList = this.GetChildControlsTabOrderList(handleCreatedOnly);
			Control[] array = new Control[childControlsTabOrderList.Count];
			for (int i = 0; i < childControlsTabOrderList.Count; i++)
			{
				array[i] = ((Control.ControlTabOrderHolder)childControlsTabOrderList[i]).control;
			}
			return array;
		}

		private static ArrayList GetChildWindows(IntPtr hWndParent)
		{
			ArrayList arrayList = new ArrayList();
			IntPtr intPtr = UnsafeNativeMethods.GetWindow(new HandleRef(null, hWndParent), 5);
			while (intPtr != IntPtr.Zero)
			{
				arrayList.Add(intPtr);
				intPtr = UnsafeNativeMethods.GetWindow(new HandleRef(null, intPtr), 2);
			}
			return arrayList;
		}

		private ArrayList GetChildWindowsTabOrderList()
		{
			ArrayList arrayList = new ArrayList();
			using (IEnumerator enumerator = Control.GetChildWindows(this.Handle).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Control control = Control.FromHandleInternal((IntPtr)enumerator.Current);
					int newOrder = (control == null) ? -1 : control.TabIndex;
					ArrayList expr_38 = arrayList;
					expr_38.Add(new Control.ControlTabOrderHolder(expr_38.Count, newOrder, control));
				}
			}
			arrayList.Sort(new Control.ControlTabOrderComparer());
			return arrayList;
		}

		internal virtual Control GetFirstChildControlInTabOrder(bool forward)
		{
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			Control control = null;
			if (controlCollection != null)
			{
				if (forward)
				{
					for (int i = 0; i < controlCollection.Count; i++)
					{
						if (control == null || control.tabIndex > controlCollection[i].tabIndex)
						{
							control = controlCollection[i];
						}
					}
				}
				else
				{
					for (int j = controlCollection.Count - 1; j >= 0; j--)
					{
						if (control == null || control.tabIndex < controlCollection[j].tabIndex)
						{
							control = controlCollection[j];
						}
					}
				}
			}
			return control;
		}

		/// <summary>Извлекает следующий или предыдущий элемент управления в последовательности табуляции дочерних элементов управления.</summary>
		/// <returns>Следующий объект <see cref="T:System.Windows.Forms.Control" /> в последовательности табуляции.</returns>
		/// <param name="ctl">Объект <see cref="T:System.Windows.Forms.Control" />, с которого следует начать поиск. </param>
		/// <param name="forward">Значение true для поиска в прямом направлении в последовательности табуляции; значение false для поиска в обратном направлении. </param>
		/// <filterpriority>2</filterpriority>
		public Control GetNextControl(Control ctl, bool forward)
		{
			if (!this.Contains(ctl))
			{
				ctl = this;
			}
			if (forward)
			{
				Control.ControlCollection controlCollection = (Control.ControlCollection)ctl.Properties.GetObject(Control.PropControlsCollection);
				if (controlCollection != null && controlCollection.Count > 0 && (ctl == this || !Control.IsFocusManagingContainerControl(ctl)))
				{
					Control firstChildControlInTabOrder = ctl.GetFirstChildControlInTabOrder(true);
					if (firstChildControlInTabOrder != null)
					{
						return firstChildControlInTabOrder;
					}
				}
				while (ctl != this)
				{
					int num = ctl.tabIndex;
					bool flag = false;
					Control control = null;
					Control arg_6E_0 = ctl.parent;
					int num2 = 0;
					Control.ControlCollection controlCollection2 = (Control.ControlCollection)arg_6E_0.Properties.GetObject(Control.PropControlsCollection);
					if (controlCollection2 != null)
					{
						num2 = controlCollection2.Count;
					}
					for (int i = 0; i < num2; i++)
					{
						if (controlCollection2[i] != ctl)
						{
							if (controlCollection2[i].tabIndex >= num && (control == null || control.tabIndex > controlCollection2[i].tabIndex) && (controlCollection2[i].tabIndex != num | flag))
							{
								control = controlCollection2[i];
							}
						}
						else
						{
							flag = true;
						}
					}
					if (control != null)
					{
						return control;
					}
					ctl = ctl.parent;
				}
			}
			else
			{
				if (ctl != this)
				{
					int num3 = ctl.tabIndex;
					bool flag2 = false;
					Control control2 = null;
					Control control3 = ctl.parent;
					int num4 = 0;
					Control.ControlCollection controlCollection3 = (Control.ControlCollection)control3.Properties.GetObject(Control.PropControlsCollection);
					if (controlCollection3 != null)
					{
						num4 = controlCollection3.Count;
					}
					for (int j = num4 - 1; j >= 0; j--)
					{
						if (controlCollection3[j] != ctl)
						{
							if (controlCollection3[j].tabIndex <= num3 && (control2 == null || control2.tabIndex < controlCollection3[j].tabIndex) && (controlCollection3[j].tabIndex != num3 | flag2))
							{
								control2 = controlCollection3[j];
							}
						}
						else
						{
							flag2 = true;
						}
					}
					if (control2 != null)
					{
						ctl = control2;
					}
					else
					{
						if (control3 == this)
						{
							return null;
						}
						return control3;
					}
				}
				Control.ControlCollection controlCollection4 = (Control.ControlCollection)ctl.Properties.GetObject(Control.PropControlsCollection);
				while (controlCollection4 != null && controlCollection4.Count > 0 && (ctl == this || !Control.IsFocusManagingContainerControl(ctl)))
				{
					Control firstChildControlInTabOrder2 = ctl.GetFirstChildControlInTabOrder(false);
					if (firstChildControlInTabOrder2 == null)
					{
						break;
					}
					ctl = firstChildControlInTabOrder2;
					controlCollection4 = (Control.ControlCollection)ctl.Properties.GetObject(Control.PropControlsCollection);
				}
			}
			if (ctl != this)
			{
				return ctl;
			}
			return null;
		}

		internal static IntPtr GetSafeHandle(IWin32Window window)
		{
			IntPtr intPtr = IntPtr.Zero;
			Control control = window as Control;
			if (control != null)
			{
				return control.Handle;
			}
			IntSecurity.AllWindows.Demand();
			intPtr = window.Handle;
			if (intPtr == IntPtr.Zero || UnsafeNativeMethods.IsWindow(new HandleRef(null, intPtr)))
			{
				return intPtr;
			}
			throw new Win32Exception(6);
		}

		internal bool GetState(int flag)
		{
			return (this.state & flag) != 0;
		}

		private bool GetState2(int flag)
		{
			return (this.state2 & flag) != 0;
		}

		/// <summary>Получает значение указанного бита стиля элемента управления для данного элемента управления.</summary>
		/// <returns>Значение true, если указанный бит стиля элемента управления имеет значение true; в противном случае — значение false.</returns>
		/// <param name="flag">Бит <see cref="T:System.Windows.Forms.ControlStyles" />, значение которого следует возвращать. </param>
		protected bool GetStyle(ControlStyles flag)
		{
			return (this.controlStyle & flag) == flag;
		}

		/// <summary>Скрывает элемент управления от пользователя.</summary>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void Hide()
		{
			this.Visible = false;
		}

		private void HookMouseEvent()
		{
			if (!this.GetState(16384))
			{
				this.SetState(16384, true);
				if (this.trackMouseEvent == null)
				{
					this.trackMouseEvent = new NativeMethods.TRACKMOUSEEVENT();
					this.trackMouseEvent.dwFlags = 3;
					this.trackMouseEvent.hwndTrack = this.Handle;
				}
				SafeNativeMethods.TrackMouseEvent(this.trackMouseEvent);
			}
		}

		/// <summary>Вызывается после добавления элемента управления в другой контейнер.</summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void InitLayout()
		{
			this.LayoutEngine.InitLayout(this, BoundsSpecified.All);
		}

		private void InitScaling(BoundsSpecified specified)
		{
			this.requiredScaling |= (byte)(specified & BoundsSpecified.All);
		}

		internal virtual IntPtr InitializeDCForWmCtlColor(IntPtr dc, int msg)
		{
			if (!this.GetStyle(ControlStyles.UserPaint))
			{
				SafeNativeMethods.SetTextColor(new HandleRef(null, dc), ColorTranslator.ToWin32(this.ForeColor));
				SafeNativeMethods.SetBkColor(new HandleRef(null, dc), ColorTranslator.ToWin32(this.BackColor));
				return this.BackColorBrush;
			}
			return UnsafeNativeMethods.GetStockObject(5);
		}

		private void InitMouseWheelSupport()
		{
			if (!Control.mouseWheelInit)
			{
				Control.mouseWheelRoutingNeeded = !SystemInformation.NativeMouseWheelSupport;
				if (Control.mouseWheelRoutingNeeded)
				{
					IntPtr arg_20_0 = IntPtr.Zero;
					if (UnsafeNativeMethods.FindWindow("MouseZ", "Magellan MSWHEEL") != IntPtr.Zero)
					{
						int num = SafeNativeMethods.RegisterWindowMessage("MSWHEEL_ROLLMSG");
						if (num != 0)
						{
							Control.mouseWheelMessage = num;
						}
					}
				}
				Control.mouseWheelInit = true;
			}
		}

		/// <summary>Делает недействительной указанную область элемента управления (добавляет ее к области обновления элемента, которая будет перерисована при следующей операции рисования) и вызывает отправку сообщения изображения элементу управления.</summary>
		/// <param name="region">Объект <see cref="T:System.Drawing.Region" />, который делается недействительным. </param>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void Invalidate(Region region)
		{
			this.Invalidate(region, false);
		}

		/// <summary>Делает недействительной указанную область элемента управления (добавляет ее к области обновления элемента, которая будет перерисована при следующей операции рисования) и вызывает отправку сообщения изображения элементу управления.При необходимости объявляет недействительными назначенные элементу управления дочерние элементы.</summary>
		/// <param name="region">Объект <see cref="T:System.Drawing.Region" />, который делается недействительным. </param>
		/// <param name="invalidateChildren">Значение true, чтобы сделать недействительными дочерние элементы управления; в противном случае — значение false. </param>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void Invalidate(Region region, bool invalidateChildren)
		{
			if (region == null)
			{
				this.Invalidate(invalidateChildren);
				return;
			}
			if (this.IsHandleCreated)
			{
				IntPtr hRgn = this.GetHRgn(region);
				try
				{
					if (invalidateChildren)
					{
						SafeNativeMethods.RedrawWindow(new HandleRef(this, this.Handle), null, new HandleRef(region, hRgn), 133);
					}
					else
					{
						using (new Control.MultithreadSafeCallScope())
						{
							SafeNativeMethods.InvalidateRgn(new HandleRef(this, this.Handle), new HandleRef(region, hRgn), !this.GetStyle(ControlStyles.Opaque));
						}
					}
				}
				finally
				{
					SafeNativeMethods.DeleteObject(new HandleRef(region, hRgn));
				}
				Rectangle invalidRect = Rectangle.Empty;
				using (Graphics graphics = this.CreateGraphicsInternal())
				{
					invalidRect = Rectangle.Ceiling(region.GetBounds(graphics));
				}
				this.OnInvalidated(new InvalidateEventArgs(invalidRect));
			}
		}

		/// <summary>Делает недействительной всю поверхность элемента управления и вызывает его перерисовку.</summary>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void Invalidate()
		{
			this.Invalidate(false);
		}

		/// <summary>Делает недействительной конкретную область элемента управления и вызывает отправку сообщения изображения элементу управления.При необходимости объявляет недействительными назначенные элементу управления дочерние элементы.</summary>
		/// <param name="invalidateChildren">Значение true, чтобы сделать недействительными дочерние элементы управления; в противном случае — значение false. </param>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void Invalidate(bool invalidateChildren)
		{
			if (this.IsHandleCreated)
			{
				if (invalidateChildren)
				{
					SafeNativeMethods.RedrawWindow(new HandleRef(this.window, this.Handle), null, NativeMethods.NullHandleRef, 133);
				}
				else
				{
					using (new Control.MultithreadSafeCallScope())
					{
						SafeNativeMethods.InvalidateRect(new HandleRef(this.window, this.Handle), null, (this.controlStyle & ControlStyles.Opaque) != ControlStyles.Opaque);
					}
				}
				this.NotifyInvalidate(this.ClientRectangle);
			}
		}

		/// <summary>Делает недействительной указанную область элемента управления (добавляет ее к области обновления элемента, которая будет перерисована при следующей операции рисования) и вызывает отправку сообщения изображения элементу управления.</summary>
		/// <param name="rc">Объект <see cref="T:System.Drawing.Rectangle" />, представляющий область, которую следует сделать недействительной. </param>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void Invalidate(Rectangle rc)
		{
			this.Invalidate(rc, false);
		}

		/// <summary>Делает недействительной указанную область элемента управления (добавляет ее к области обновления элемента, которая будет перерисована при следующей операции рисования) и вызывает отправку сообщения изображения элементу управления.При необходимости объявляет недействительными назначенные элементу управления дочерние элементы.</summary>
		/// <param name="rc">Объект <see cref="T:System.Drawing.Rectangle" />, представляющий область, которую следует сделать недействительной. </param>
		/// <param name="invalidateChildren">Значение true, чтобы сделать недействительными дочерние элементы управления; в противном случае — значение false. </param>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void Invalidate(Rectangle rc, bool invalidateChildren)
		{
			if (rc.IsEmpty)
			{
				this.Invalidate(invalidateChildren);
				return;
			}
			if (this.IsHandleCreated)
			{
				if (invalidateChildren)
				{
					NativeMethods.RECT rECT = NativeMethods.RECT.FromXYWH(rc.X, rc.Y, rc.Width, rc.Height);
					SafeNativeMethods.RedrawWindow(new HandleRef(this.window, this.Handle), ref rECT, NativeMethods.NullHandleRef, 133);
				}
				else
				{
					NativeMethods.RECT rECT2 = NativeMethods.RECT.FromXYWH(rc.X, rc.Y, rc.Width, rc.Height);
					using (new Control.MultithreadSafeCallScope())
					{
						SafeNativeMethods.InvalidateRect(new HandleRef(this.window, this.Handle), ref rECT2, (this.controlStyle & ControlStyles.Opaque) != ControlStyles.Opaque);
					}
				}
				this.NotifyInvalidate(rc);
			}
		}

		/// <summary>Выполняет указанный делегат в том потоке, которому принадлежит базовый дескриптор окна элемента управления.</summary>
		/// <returns>Значение, возвращаемое вызываемым делегатом, или значение null, если делегат не возвращает никакого значения.</returns>
		/// <param name="method">Делегат, содержащий метод, который требуется вызывать в контексте потока элемента управления. </param>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public object Invoke(Delegate method)
		{
			return this.Invoke(method, null);
		}

		/// <summary>Выполняет указанный делегат в том потоке, которому принадлежит основной дескриптор окна элемента управления, с указанным списком аргументов.</summary>
		/// <returns>Объект <see cref="T:System.Object" />, содержащий возвращаемое значение от вызываемого делегата, или значение null, если делегат не имеет возвращаемого значения.</returns>
		/// <param name="method">Делегат метода, принимающий параметры, количество и тип которых является таким же, что и в параметре <paramref name="args" />. </param>
		/// <param name="args">Массив объектов, передаваемых в качестве аргументов указанному методу.Данный параметр может иметь значение null, если метод не принимает аргументы.</param>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public object Invoke(Delegate method, params object[] args)
		{
			object result;
			using (new Control.MultithreadSafeCallScope())
			{
				result = this.FindMarshalingControl().MarshaledInvoke(this, method, args, true);
			}
			return result;
		}

		private void InvokeMarshaledCallback(Control.ThreadMethodEntry tme)
		{
			if (tme.executionContext != null)
			{
				if (Control.invokeMarshaledCallbackHelperDelegate == null)
				{
					Control.invokeMarshaledCallbackHelperDelegate = new ContextCallback(Control.InvokeMarshaledCallbackHelper);
				}
				if (SynchronizationContext.Current == null)
				{
					WindowsFormsSynchronizationContext.InstallIfNeeded();
				}
				tme.syncContext = SynchronizationContext.Current;
				ExecutionContext.Run(tme.executionContext, Control.invokeMarshaledCallbackHelperDelegate, tme);
				return;
			}
			Control.InvokeMarshaledCallbackHelper(tme);
		}

		private static void InvokeMarshaledCallbackHelper(object obj)
		{
			Control.ThreadMethodEntry threadMethodEntry = (Control.ThreadMethodEntry)obj;
			if (threadMethodEntry.syncContext != null)
			{
				SynchronizationContext current = SynchronizationContext.Current;
				try
				{
					SynchronizationContext.SetSynchronizationContext(threadMethodEntry.syncContext);
					Control.InvokeMarshaledCallbackDo(threadMethodEntry);
					return;
				}
				finally
				{
					SynchronizationContext.SetSynchronizationContext(current);
				}
			}
			Control.InvokeMarshaledCallbackDo(threadMethodEntry);
		}

		private static void InvokeMarshaledCallbackDo(Control.ThreadMethodEntry tme)
		{
			if (tme.method is EventHandler)
			{
				if (tme.args == null || tme.args.Length < 1)
				{
					((EventHandler)tme.method)(tme.caller, EventArgs.Empty);
					return;
				}
				if (tme.args.Length < 2)
				{
					((EventHandler)tme.method)(tme.args[0], EventArgs.Empty);
					return;
				}
				((EventHandler)tme.method)(tme.args[0], (EventArgs)tme.args[1]);
				return;
			}
			else
			{
				if (tme.method is MethodInvoker)
				{
					((MethodInvoker)tme.method)();
					return;
				}
				if (tme.method is WaitCallback)
				{
					((WaitCallback)tme.method)(tme.args[0]);
					return;
				}
				tme.retVal = tme.method.DynamicInvoke(tme.args);
				return;
			}
		}

		private void InvokeMarshaledCallbacks()
		{
			Control.ThreadMethodEntry threadMethodEntry = null;
			Queue obj = this.threadCallbackList;
			lock (obj)
			{
				if (this.threadCallbackList.Count > 0)
				{
					threadMethodEntry = (Control.ThreadMethodEntry)this.threadCallbackList.Dequeue();
				}
				goto IL_E3;
			}
			IL_41:
			if (threadMethodEntry.method != null)
			{
				try
				{
					if (NativeWindow.WndProcShouldBeDebuggable && !threadMethodEntry.synchronous)
					{
						this.InvokeMarshaledCallback(threadMethodEntry);
					}
					else
					{
						try
						{
							this.InvokeMarshaledCallback(threadMethodEntry);
						}
						catch (Exception ex)
						{
							threadMethodEntry.exception = ex.GetBaseException();
						}
					}
				}
				finally
				{
					threadMethodEntry.Complete();
					if (!NativeWindow.WndProcShouldBeDebuggable && threadMethodEntry.exception != null && !threadMethodEntry.synchronous)
					{
						Application.OnThreadException(threadMethodEntry.exception);
					}
				}
			}
			obj = this.threadCallbackList;
			lock (obj)
			{
				if (this.threadCallbackList.Count > 0)
				{
					threadMethodEntry = (Control.ThreadMethodEntry)this.threadCallbackList.Dequeue();
				}
				else
				{
					threadMethodEntry = null;
				}
			}
			IL_E3:
			if (threadMethodEntry == null)
			{
				return;
			}
			goto IL_41;
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.Paint" /> для указанного элемента управления.</summary>
		/// <param name="c">Объект <see cref="T:System.Windows.Forms.Control" />, которому следует назначить событие <see cref="E:System.Windows.Forms.Control.Paint" />. </param>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.PaintEventArgs" />, содержащий данные события. </param>
		protected void InvokePaint(Control c, PaintEventArgs e)
		{
			c.OnPaint(e);
		}

		/// <summary>Вызывает событие PaintBackground для указанного элемента управления.</summary>
		/// <param name="c">Объект <see cref="T:System.Windows.Forms.Control" />, которому следует назначить событие <see cref="E:System.Windows.Forms.Control.Paint" />. </param>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.PaintEventArgs" />, содержащий данные события. </param>
		protected void InvokePaintBackground(Control c, PaintEventArgs e)
		{
			c.OnPaintBackground(e);
		}

		internal bool IsFontSet()
		{
			return (Font)this.Properties.GetObject(Control.PropFont) != null;
		}

		internal bool IsDescendant(Control descendant)
		{
			for (Control control = descendant; control != null; control = control.ParentInternal)
			{
				if (control == this)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Определяет, задействованы ли клавиши CAPS LOCK, NUM LOCK или SCROLL LOCK.</summary>
		/// <returns>Значение true, если указанные клавиши задействованы, в противном случае — значение false.</returns>
		/// <param name="keyVal">Элемент перечисления <see cref="T:System.Windows.Forms.Keys" />: CAPS LOCK, NUM LOCK или SCROLL LOCK. </param>
		/// <exception cref="T:System.NotSupportedException">Параметр <paramref name="keyVal" /> относится к клавишам, отличным от клавиш CAPS LOCK, NUM LOCK или SCROLL LOCK. </exception>
		/// <filterpriority>2</filterpriority>
		public static bool IsKeyLocked(Keys keyVal)
		{
			if (keyVal != Keys.Insert && keyVal != Keys.NumLock && keyVal != Keys.Capital && keyVal != Keys.Scroll)
			{
				throw new NotSupportedException(SR.GetString("ControlIsKeyLockedNumCapsScrollLockKeysSupportedOnly"));
			}
			int keyState = (int)UnsafeNativeMethods.GetKeyState((int)keyVal);
			if (keyVal == Keys.Insert || keyVal == Keys.Capital)
			{
				return (keyState & 1) != 0;
			}
			return (keyState & 32769) != 0;
		}

		/// <summary>Определяет, является ли символ входным символом, который распознается элементом управления.</summary>
		/// <returns>Значение true, если символ должен быть отправлен непосредственно в элемент управления без предварительной обработки; в противном случае — значение false.</returns>
		/// <param name="charCode">Проверяемый символ. </param>
		[UIPermission(SecurityAction.InheritanceDemand, Window = UIPermissionWindow.AllWindows)]
		protected virtual bool IsInputChar(char charCode)
		{
			int num;
			if (charCode == '\t')
			{
				num = 134;
			}
			else
			{
				num = 132;
			}
			return ((int)((long)this.SendMessage(135, 0, 0)) & num) != 0;
		}

		/// <summary>Определяет, является ли заданная клавиша стандартной клавишей ввода или специальной клавишей, требующей предварительной обработки.</summary>
		/// <returns>Значение true, если указанная клавиша является стандартной клавишей ввода; в противном случае — значение false.</returns>
		/// <param name="keyData">Одно из значений <see cref="T:System.Windows.Forms.Keys" />. </param>
		[UIPermission(SecurityAction.InheritanceDemand, Window = UIPermissionWindow.AllWindows)]
		protected virtual bool IsInputKey(Keys keyData)
		{
			if ((keyData & Keys.Alt) == Keys.Alt)
			{
				return false;
			}
			int num = 4;
			Keys keys = keyData & Keys.KeyCode;
			if (keys != Keys.Tab)
			{
				switch (keys)
				{
				case Keys.Left:
				case Keys.Up:
				case Keys.Right:
				case Keys.Down:
					num = 5;
					break;
				}
			}
			else
			{
				num = 6;
			}
			return this.IsHandleCreated && ((int)((long)this.SendMessage(135, 0, 0)) & num) != 0;
		}

		/// <summary>Определяет, является ли указанный символ назначенным символом для элемента управления в заданной строке.</summary>
		/// <returns>Значение true, если символ <paramref name="charCode" /> является назначенным символом для элемента управления; в противном случае — значение false.</returns>
		/// <param name="charCode">Проверяемый символ. </param>
		/// <param name="text">Строка для поиска. </param>
		/// <filterpriority>2</filterpriority>
		public static bool IsMnemonic(char charCode, string text)
		{
			if (charCode == '&')
			{
				return false;
			}
			if (text != null)
			{
				int num = -1;
				char c = char.ToUpper(charCode, CultureInfo.CurrentCulture);
				while (num + 1 < text.Length)
				{
					num = text.IndexOf('&', num + 1) + 1;
					if (num <= 0 || num >= text.Length)
					{
						break;
					}
					char c2 = char.ToUpper(text[num], CultureInfo.CurrentCulture);
					if (c2 == c || char.ToLower(c2, CultureInfo.CurrentCulture) == char.ToLower(c, CultureInfo.CurrentCulture))
					{
						return true;
					}
				}
			}
			return false;
		}

		private void ListenToUserPreferenceChanged(bool listen)
		{
			if (this.GetState2(4))
			{
				if (!listen)
				{
					this.SetState2(4, false);
					SystemEvents.UserPreferenceChanged -= new UserPreferenceChangedEventHandler(this.UserPreferenceChanged);
					return;
				}
			}
			else if (listen)
			{
				this.SetState2(4, true);
				SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(this.UserPreferenceChanged);
			}
		}

		private object MarshaledInvoke(Control caller, Delegate method, object[] args, bool synchronous)
		{
			if (!this.IsHandleCreated)
			{
				throw new InvalidOperationException(SR.GetString("ErrorNoMarshalingThread"));
			}
			if ((Control.ActiveXImpl)this.Properties.GetObject(Control.PropActiveXImpl) != null)
			{
				IntSecurity.UnmanagedCode.Demand();
			}
			bool flag = false;
			int num;
			if (SafeNativeMethods.GetWindowThreadProcessId(new HandleRef(this, this.Handle), out num) == SafeNativeMethods.GetCurrentThreadId() && synchronous)
			{
				flag = true;
			}
			ExecutionContext executionContext = null;
			if (!flag)
			{
				executionContext = ExecutionContext.Capture();
			}
			Control.ThreadMethodEntry threadMethodEntry = new Control.ThreadMethodEntry(caller, this, method, args, synchronous, executionContext);
			lock (this)
			{
				if (this.threadCallbackList == null)
				{
					this.threadCallbackList = new Queue();
				}
			}
			Queue obj = this.threadCallbackList;
			lock (obj)
			{
				if (Control.threadCallbackMessage == 0)
				{
					Control.threadCallbackMessage = SafeNativeMethods.RegisterWindowMessage(Application.WindowMessagesVersion + "_ThreadCallbackMessage");
				}
				this.threadCallbackList.Enqueue(threadMethodEntry);
			}
			if (flag)
			{
				this.InvokeMarshaledCallbacks();
			}
			else
			{
				UnsafeNativeMethods.PostMessage(new HandleRef(this, this.Handle), Control.threadCallbackMessage, IntPtr.Zero, IntPtr.Zero);
			}
			if (!synchronous)
			{
				return threadMethodEntry;
			}
			if (!threadMethodEntry.IsCompleted)
			{
				this.WaitForWaitHandle(threadMethodEntry.AsyncWaitHandle);
			}
			if (threadMethodEntry.exception != null)
			{
				throw threadMethodEntry.exception;
			}
			return threadMethodEntry.retVal;
		}

		private void MarshalStringToMessage(string value, ref Message m)
		{
			if (m.LParam == IntPtr.Zero)
			{
				m.Result = (IntPtr)((value.Length + 1) * Marshal.SystemDefaultCharSize);
				return;
			}
			if ((int)((long)m.WParam) < value.Length + 1)
			{
				m.Result = (IntPtr)(-1);
				return;
			}
			char[] chars = new char[1];
			byte[] bytes;
			byte[] bytes2;
			if (Marshal.SystemDefaultCharSize == 1)
			{
				bytes = Encoding.Default.GetBytes(value);
				bytes2 = Encoding.Default.GetBytes(chars);
			}
			else
			{
				bytes = Encoding.Unicode.GetBytes(value);
				bytes2 = Encoding.Unicode.GetBytes(chars);
			}
			Marshal.Copy(bytes, 0, m.LParam, bytes.Length);
			Marshal.Copy(bytes2, 0, (IntPtr)((long)m.LParam + (long)bytes.Length), bytes2.Length);
			m.Result = (IntPtr)((bytes.Length + bytes2.Length) / Marshal.SystemDefaultCharSize);
		}

		internal void NotifyEnter()
		{
			this.OnEnter(EventArgs.Empty);
		}

		internal void NotifyLeave()
		{
			this.OnLeave(EventArgs.Empty);
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.Invalidated" />, чтобы сделать недействительной указанную область элемента управления.</summary>
		/// <param name="invalidatedArea">Объект <see cref="T:System.Drawing.Rectangle" />, представляющий область, которую требуется сделать недопустимой. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void NotifyInvalidate(Rectangle invalidatedArea)
		{
			this.OnInvalidated(new InvalidateEventArgs(invalidatedArea));
		}

		private bool NotifyValidating()
		{
			CancelEventArgs cancelEventArgs = new CancelEventArgs();
			this.OnValidating(cancelEventArgs);
			return cancelEventArgs.Cancel;
		}

		private void NotifyValidated()
		{
			this.OnValidated(EventArgs.Empty);
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.Click" /> для указанного элемента управления.</summary>
		/// <param name="toInvoke">Объект <see cref="T:System.Windows.Forms.Control" />, которому следует назначить событие <see cref="E:System.Windows.Forms.Control.Click" />. </param>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void InvokeOnClick(Control toInvoke, EventArgs e)
		{
			if (toInvoke != null)
			{
				toInvoke.OnClick(e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.AutoSizeChanged" />. </summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события.</param>
		protected virtual void OnAutoSizeChanged(EventArgs e)
		{
			EventHandler eventHandler = base.Events[Control.EventAutoSizeChanged] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.BackColorChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnBackColorChanged(EventArgs e)
		{
			if (this.GetAnyDisposingInHierarchy())
			{
				return;
			}
			object @object = this.Properties.GetObject(Control.PropBackBrush);
			if (@object != null)
			{
				if (this.GetState(2097152))
				{
					IntPtr intPtr = (IntPtr)@object;
					if (intPtr != IntPtr.Zero)
					{
						SafeNativeMethods.DeleteObject(new HandleRef(this, intPtr));
					}
				}
				this.Properties.SetObject(Control.PropBackBrush, null);
			}
			this.Invalidate();
			EventHandler eventHandler = base.Events[Control.EventBackColor] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			if (controlCollection != null)
			{
				for (int i = 0; i < controlCollection.Count; i++)
				{
					controlCollection[i].OnParentBackColorChanged(e);
				}
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.BackgroundImageChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnBackgroundImageChanged(EventArgs e)
		{
			if (this.GetAnyDisposingInHierarchy())
			{
				return;
			}
			this.Invalidate();
			EventHandler eventHandler = base.Events[Control.EventBackgroundImage] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			if (controlCollection != null)
			{
				for (int i = 0; i < controlCollection.Count; i++)
				{
					controlCollection[i].OnParentBackgroundImageChanged(e);
				}
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.BackgroundImageLayoutChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnBackgroundImageLayoutChanged(EventArgs e)
		{
			if (this.GetAnyDisposingInHierarchy())
			{
				return;
			}
			this.Invalidate();
			EventHandler eventHandler = base.Events[Control.EventBackgroundImageLayout] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.BindingContextChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnBindingContextChanged(EventArgs e)
		{
			if (this.Properties.GetObject(Control.PropBindings) != null)
			{
				this.UpdateBindings();
			}
			EventHandler eventHandler = base.Events[Control.EventBindingContext] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			if (controlCollection != null)
			{
				for (int i = 0; i < controlCollection.Count; i++)
				{
					controlCollection[i].OnParentBindingContextChanged(e);
				}
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.CausesValidationChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnCausesValidationChanged(EventArgs e)
		{
			EventHandler eventHandler = base.Events[Control.EventCausesValidation] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		internal virtual void OnChildLayoutResuming(Control child, bool performLayout)
		{
			if (this.ParentInternal != null)
			{
				this.ParentInternal.OnChildLayoutResuming(child, performLayout);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.ContextMenuChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnContextMenuChanged(EventArgs e)
		{
			EventHandler eventHandler = base.Events[Control.EventContextMenu] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.ContextMenuStripChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события.</param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnContextMenuStripChanged(EventArgs e)
		{
			EventHandler eventHandler = base.Events[Control.EventContextMenuStrip] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.CursorChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnCursorChanged(EventArgs e)
		{
			EventHandler eventHandler = base.Events[Control.EventCursor] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			if (controlCollection != null)
			{
				for (int i = 0; i < controlCollection.Count; i++)
				{
					controlCollection[i].OnParentCursorChanged(e);
				}
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.DockChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnDockChanged(EventArgs e)
		{
			EventHandler eventHandler = base.Events[Control.EventDock] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.EnabledChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnEnabledChanged(EventArgs e)
		{
			if (this.GetAnyDisposingInHierarchy())
			{
				return;
			}
			if (this.IsHandleCreated)
			{
				SafeNativeMethods.EnableWindow(new HandleRef(this, this.Handle), this.Enabled);
				if (this.GetStyle(ControlStyles.UserPaint))
				{
					this.Invalidate();
					this.Update();
				}
			}
			EventHandler eventHandler = base.Events[Control.EventEnabled] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			if (controlCollection != null)
			{
				for (int i = 0; i < controlCollection.Count; i++)
				{
					controlCollection[i].OnParentEnabledChanged(e);
				}
			}
		}

		internal virtual void OnFrameWindowActivate(bool fActivate)
		{
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.FontChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnFontChanged(EventArgs e)
		{
			if (this.GetAnyDisposingInHierarchy())
			{
				return;
			}
			this.Invalidate();
			if (this.Properties.ContainsInteger(Control.PropFontHeight))
			{
				this.Properties.SetInteger(Control.PropFontHeight, -1);
			}
			this.DisposeFontHandle();
			if (this.IsHandleCreated && !this.GetStyle(ControlStyles.UserPaint))
			{
				this.SetWindowFont();
			}
			EventHandler eventHandler = base.Events[Control.EventFont] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			using (new LayoutTransaction(this, this, PropertyNames.Font, false))
			{
				if (controlCollection != null)
				{
					for (int i = 0; i < controlCollection.Count; i++)
					{
						controlCollection[i].OnParentFontChanged(e);
					}
				}
			}
			LayoutTransaction.DoLayout(this, this, PropertyNames.Font);
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.ForeColorChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnForeColorChanged(EventArgs e)
		{
			if (this.GetAnyDisposingInHierarchy())
			{
				return;
			}
			this.Invalidate();
			EventHandler eventHandler = base.Events[Control.EventForeColor] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			if (controlCollection != null)
			{
				for (int i = 0; i < controlCollection.Count; i++)
				{
					controlCollection[i].OnParentForeColorChanged(e);
				}
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.RightToLeftChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnRightToLeftChanged(EventArgs e)
		{
			if (this.GetAnyDisposingInHierarchy())
			{
				return;
			}
			this.SetState2(2, true);
			this.RecreateHandle();
			EventHandler eventHandler = base.Events[Control.EventRightToLeft] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			if (controlCollection != null)
			{
				for (int i = 0; i < controlCollection.Count; i++)
				{
					controlCollection[i].OnParentRightToLeftChanged(e);
				}
			}
		}

		/// <summary>Уведомляет элемент управления о сообщениях Windows.</summary>
		/// <param name="m">Объект <see cref="T:System.Windows.Forms.Message" />, представляющий сообщение Windows. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnNotifyMessage(Message m)
		{
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.BackColorChanged" /> при изменении значения свойства <see cref="P:System.Windows.Forms.Control.BackColor" /> контейнера элемента управления.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnParentBackColorChanged(EventArgs e)
		{
			if (this.Properties.GetColor(Control.PropBackColor).IsEmpty)
			{
				this.OnBackColorChanged(e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.BackgroundImageChanged" /> при изменении значения свойства <see cref="P:System.Windows.Forms.Control.BackgroundImage" /> контейнера элемента управления.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnParentBackgroundImageChanged(EventArgs e)
		{
			this.OnBackgroundImageChanged(e);
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.BindingContextChanged" /> при изменении значения свойства <see cref="P:System.Windows.Forms.Control.BindingContext" /> контейнера элемента управления.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnParentBindingContextChanged(EventArgs e)
		{
			if (this.Properties.GetObject(Control.PropBindingManager) == null)
			{
				this.OnBindingContextChanged(e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.CursorChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события.</param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnParentCursorChanged(EventArgs e)
		{
			if (this.Properties.GetObject(Control.PropCursor) == null)
			{
				this.OnCursorChanged(e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.EnabledChanged" /> при изменении значения свойства <see cref="P:System.Windows.Forms.Control.Enabled" /> контейнера элемента управления.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnParentEnabledChanged(EventArgs e)
		{
			if (this.GetState(4))
			{
				this.OnEnabledChanged(e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.FontChanged" /> при изменении значения свойства <see cref="P:System.Windows.Forms.Control.Font" /> контейнера элемента управления.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnParentFontChanged(EventArgs e)
		{
			if (this.Properties.GetObject(Control.PropFont) == null)
			{
				this.OnFontChanged(e);
			}
		}

		internal virtual void OnParentHandleRecreated()
		{
			Control parentInternal = this.ParentInternal;
			if (parentInternal != null && this.IsHandleCreated)
			{
				UnsafeNativeMethods.SetParent(new HandleRef(this, this.Handle), new HandleRef(parentInternal, parentInternal.Handle));
				this.UpdateZOrder();
			}
			this.SetState(536870912, false);
			if (this.ReflectParent == this.ParentInternal)
			{
				this.RecreateHandle();
			}
		}

		internal virtual void OnParentHandleRecreating()
		{
			this.SetState(536870912, true);
			if (this.IsHandleCreated)
			{
				Application.ParkHandle(new HandleRef(this, this.Handle));
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.ForeColorChanged" /> при изменении значения свойства <see cref="P:System.Windows.Forms.Control.ForeColor" /> контейнера элемента управления.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnParentForeColorChanged(EventArgs e)
		{
			if (this.Properties.GetColor(Control.PropForeColor).IsEmpty)
			{
				this.OnForeColorChanged(e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.RightToLeftChanged" /> при изменении значения свойства <see cref="P:System.Windows.Forms.Control.RightToLeft" /> контейнера элемента управления.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnParentRightToLeftChanged(EventArgs e)
		{
			if (!this.Properties.ContainsInteger(Control.PropRightToLeft) || this.Properties.GetInteger(Control.PropRightToLeft) == 2)
			{
				this.OnRightToLeftChanged(e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.VisibleChanged" /> при изменении значения свойства <see cref="P:System.Windows.Forms.Control.Visible" /> контейнера элемента управления.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnParentVisibleChanged(EventArgs e)
		{
			if (this.GetState(2))
			{
				this.OnVisibleChanged(e);
			}
		}

		internal virtual void OnParentBecameInvisible()
		{
			if (this.GetState(2))
			{
				Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
				if (controlCollection != null)
				{
					for (int i = 0; i < controlCollection.Count; i++)
					{
						controlCollection[i].OnParentBecameInvisible();
					}
				}
			}
		}

		/// <summary>Генерирует событие <see cref="E:System.Windows.Forms.Control.Paint" />. </summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.PaintEventArgs" />, содержащий данные, относящиеся к событию.</param>
		/// <exception cref="T:System.ArgumentNullException">Значение параметра <paramref name="e" /> — null.</exception>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnPrint(PaintEventArgs e)
		{
			if (e == null)
			{
				throw new ArgumentNullException("e");
			}
			if (this.GetStyle(ControlStyles.UserPaint))
			{
				this.PaintWithErrorHandling(e, 1);
				e.ResetGraphics();
				this.PaintWithErrorHandling(e, 2);
				return;
			}
			Control.PrintPaintEventArgs printPaintEventArgs = e as Control.PrintPaintEventArgs;
			bool flag = false;
			IntPtr intPtr = IntPtr.Zero;
			Message message;
			if (printPaintEventArgs == null)
			{
				IntPtr lparam = (IntPtr)30;
				intPtr = e.HDC;
				if (intPtr == IntPtr.Zero)
				{
					intPtr = e.Graphics.GetHdc();
					flag = true;
				}
				message = Message.Create(this.Handle, 792, intPtr, lparam);
			}
			else
			{
				message = printPaintEventArgs.Message;
			}
			try
			{
				this.DefWndProc(ref message);
			}
			finally
			{
				if (flag)
				{
					e.Graphics.ReleaseHdcInternal(intPtr);
				}
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.TabIndexChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnTabIndexChanged(EventArgs e)
		{
			EventHandler eventHandler = base.Events[Control.EventTabIndex] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.TabStopChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnTabStopChanged(EventArgs e)
		{
			EventHandler eventHandler = base.Events[Control.EventTabStop] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.TextChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnTextChanged(EventArgs e)
		{
			EventHandler eventHandler = base.Events[Control.EventText] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.VisibleChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnVisibleChanged(EventArgs e)
		{
			bool visible = this.Visible;
			if (visible)
			{
				this.UnhookMouseEvent();
				this.trackMouseEvent = null;
			}
			if ((this.parent != null & visible) && !this.Created && !this.GetAnyDisposingInHierarchy())
			{
				this.CreateControl();
			}
			EventHandler eventHandler = base.Events[Control.EventVisible] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			if (controlCollection != null)
			{
				for (int i = 0; i < controlCollection.Count; i++)
				{
					Control control = controlCollection[i];
					if (control.Visible)
					{
						control.OnParentVisibleChanged(e);
					}
					if (!visible)
					{
						control.OnParentBecameInvisible();
					}
				}
			}
		}

		internal virtual void OnTopMostActiveXParentChanged(EventArgs e)
		{
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			if (controlCollection != null)
			{
				for (int i = 0; i < controlCollection.Count; i++)
				{
					controlCollection[i].OnTopMostActiveXParentChanged(e);
				}
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.ParentChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnParentChanged(EventArgs e)
		{
			EventHandler eventHandler = base.Events[Control.EventParent] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			if (this.TopMostParent.IsActiveX)
			{
				this.OnTopMostActiveXParentChanged(EventArgs.Empty);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.Click" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnClick(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventClick];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.ClientSizeChanged" />. </summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnClientSizeChanged(EventArgs e)
		{
			EventHandler eventHandler = base.Events[Control.EventClientSize] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.ControlAdded" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.ControlEventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnControlAdded(ControlEventArgs e)
		{
			ControlEventHandler controlEventHandler = (ControlEventHandler)base.Events[Control.EventControlAdded];
			if (controlEventHandler != null)
			{
				controlEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.ControlRemoved" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.ControlEventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnControlRemoved(ControlEventArgs e)
		{
			ControlEventHandler controlEventHandler = (ControlEventHandler)base.Events[Control.EventControlRemoved];
			if (controlEventHandler != null)
			{
				controlEventHandler(this, e);
			}
		}

		/// <summary>Вызывает метод <see cref="M:System.Windows.Forms.Control.CreateControl" />.</summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnCreateControl()
		{
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.HandleCreated" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnHandleCreated(EventArgs e)
		{
			if (this.IsHandleCreated)
			{
				if (!this.GetStyle(ControlStyles.UserPaint))
				{
					this.SetWindowFont();
				}
				this.SetAcceptDrops(this.AllowDrop);
				Region region = (Region)this.Properties.GetObject(Control.PropRegion);
				if (region != null)
				{
					IntPtr intPtr = this.GetHRgn(region);
					try
					{
						if (this.IsActiveX)
						{
							intPtr = this.ActiveXMergeRegion(intPtr);
						}
						if (UnsafeNativeMethods.SetWindowRgn(new HandleRef(this, this.Handle), new HandleRef(this, intPtr), SafeNativeMethods.IsWindowVisible(new HandleRef(this, this.Handle))) != 0)
						{
							intPtr = IntPtr.Zero;
						}
					}
					finally
					{
						if (intPtr != IntPtr.Zero)
						{
							SafeNativeMethods.DeleteObject(new HandleRef(null, intPtr));
						}
					}
				}
				Control.ControlAccessibleObject controlAccessibleObject = this.Properties.GetObject(Control.PropAccessibility) as Control.ControlAccessibleObject;
				Control.ControlAccessibleObject controlAccessibleObject2 = this.Properties.GetObject(Control.PropNcAccessibility) as Control.ControlAccessibleObject;
				IntPtr handle = this.Handle;
				IntSecurity.UnmanagedCode.Assert();
				try
				{
					if (controlAccessibleObject != null)
					{
						controlAccessibleObject.Handle = handle;
					}
					if (controlAccessibleObject2 != null)
					{
						controlAccessibleObject2.Handle = handle;
					}
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
				}
				if (this.text != null && this.text.Length != 0)
				{
					UnsafeNativeMethods.SetWindowText(new HandleRef(this, this.Handle), this.text);
				}
				if (!(this is ScrollableControl) && !this.IsMirrored && this.GetState2(2) && !this.GetState2(1))
				{
					this.BeginInvoke(new EventHandler(this.OnSetScrollPosition));
					this.SetState2(1, true);
					this.SetState2(2, false);
				}
				if (this.GetState2(8))
				{
					this.ListenToUserPreferenceChanged(this.GetTopLevel());
				}
			}
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventHandleCreated];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			if (this.IsHandleCreated && this.GetState(32768))
			{
				UnsafeNativeMethods.PostMessage(new HandleRef(this, this.Handle), Control.threadCallbackMessage, IntPtr.Zero, IntPtr.Zero);
				this.SetState(32768, false);
			}
		}

		private void OnSetScrollPosition(object sender, EventArgs e)
		{
			this.SetState2(1, false);
			this.OnInvokedSetScrollPosition(sender, e);
		}

		internal virtual void OnInvokedSetScrollPosition(object sender, EventArgs e)
		{
			if (!(this is ScrollableControl) && !this.IsMirrored)
			{
				NativeMethods.SCROLLINFO sCROLLINFO = new NativeMethods.SCROLLINFO();
				sCROLLINFO.cbSize = Marshal.SizeOf(typeof(NativeMethods.SCROLLINFO));
				sCROLLINFO.fMask = 1;
				if (UnsafeNativeMethods.GetScrollInfo(new HandleRef(this, this.Handle), 0, sCROLLINFO))
				{
					sCROLLINFO.nPos = ((this.RightToLeft == RightToLeft.Yes) ? sCROLLINFO.nMax : sCROLLINFO.nMin);
					this.SendMessage(276, NativeMethods.Util.MAKELPARAM(4, sCROLLINFO.nPos), 0);
				}
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.LocationChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnLocationChanged(EventArgs e)
		{
			this.OnMove(EventArgs.Empty);
			EventHandler eventHandler = base.Events[Control.EventLocation] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.HandleDestroyed" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnHandleDestroyed(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventHandleDestroyed];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			this.UpdateReflectParent(false);
			if (!this.RecreatingHandle)
			{
				if (this.GetState(2097152))
				{
					object @object = this.Properties.GetObject(Control.PropBackBrush);
					if (@object != null)
					{
						IntPtr intPtr = (IntPtr)@object;
						if (intPtr != IntPtr.Zero)
						{
							SafeNativeMethods.DeleteObject(new HandleRef(this, intPtr));
						}
						this.Properties.SetObject(Control.PropBackBrush, null);
					}
				}
				this.ListenToUserPreferenceChanged(false);
			}
			try
			{
				if (!this.GetAnyDisposingInHierarchy())
				{
					this.text = this.Text;
					if (this.text != null && this.text.Length == 0)
					{
						this.text = null;
					}
				}
				this.SetAcceptDrops(false);
			}
			catch (Exception arg_C4_0)
			{
				if (ClientUtils.IsSecurityOrCriticalException(arg_C4_0))
				{
					throw;
				}
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.DoubleClick" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnDoubleClick(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventDoubleClick];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.DragEnter" />.</summary>
		/// <param name="drgevent">Объект <see cref="T:System.Windows.Forms.DragEventArgs" />, содержащий данные, относящиеся к событию. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnDragEnter(DragEventArgs drgevent)
		{
			DragEventHandler dragEventHandler = (DragEventHandler)base.Events[Control.EventDragEnter];
			if (dragEventHandler != null)
			{
				dragEventHandler(this, drgevent);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.DragOver" />.</summary>
		/// <param name="drgevent">Объект <see cref="T:System.Windows.Forms.DragEventArgs" />, содержащий данные, относящиеся к событию. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnDragOver(DragEventArgs drgevent)
		{
			DragEventHandler dragEventHandler = (DragEventHandler)base.Events[Control.EventDragOver];
			if (dragEventHandler != null)
			{
				dragEventHandler(this, drgevent);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.DragLeave" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnDragLeave(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventDragLeave];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.DragDrop" />.</summary>
		/// <param name="drgevent">Объект <see cref="T:System.Windows.Forms.DragEventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnDragDrop(DragEventArgs drgevent)
		{
			DragEventHandler dragEventHandler = (DragEventHandler)base.Events[Control.EventDragDrop];
			if (dragEventHandler != null)
			{
				dragEventHandler(this, drgevent);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.GiveFeedback" />.</summary>
		/// <param name="gfbevent">Объект <see cref="T:System.Windows.Forms.GiveFeedbackEventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnGiveFeedback(GiveFeedbackEventArgs gfbevent)
		{
			GiveFeedbackEventHandler giveFeedbackEventHandler = (GiveFeedbackEventHandler)base.Events[Control.EventGiveFeedback];
			if (giveFeedbackEventHandler != null)
			{
				giveFeedbackEventHandler(this, gfbevent);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.Enter" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnEnter(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventEnter];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.GotFocus" /> для указанного элемента управления.</summary>
		/// <param name="toInvoke">Объект <see cref="T:System.Windows.Forms.Control" />, которому следует назначить событие. </param>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void InvokeGotFocus(Control toInvoke, EventArgs e)
		{
			if (toInvoke != null)
			{
				toInvoke.OnGotFocus(e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.GotFocus" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnGotFocus(EventArgs e)
		{
			if (this.IsActiveX)
			{
				this.ActiveXOnFocus(true);
			}
			if (this.parent != null)
			{
				this.parent.ChildGotFocus(this);
			}
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventGotFocus];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.HelpRequested" />.</summary>
		/// <param name="hevent">Объект <see cref="T:System.Windows.Forms.HelpEventArgs" />, содержащий данные, относящиеся к событию. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnHelpRequested(HelpEventArgs hevent)
		{
			HelpEventHandler helpEventHandler = (HelpEventHandler)base.Events[Control.EventHelpRequested];
			if (helpEventHandler != null)
			{
				helpEventHandler(this, hevent);
				hevent.Handled = true;
			}
			if (!hevent.Handled && this.ParentInternal != null)
			{
				this.ParentInternal.OnHelpRequested(hevent);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.Invalidated" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.InvalidateEventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnInvalidated(InvalidateEventArgs e)
		{
			if (this.IsActiveX)
			{
				this.ActiveXViewChanged();
			}
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			if (controlCollection != null)
			{
				for (int i = 0; i < controlCollection.Count; i++)
				{
					controlCollection[i].OnParentInvalidated(e);
				}
			}
			InvalidateEventHandler invalidateEventHandler = (InvalidateEventHandler)base.Events[Control.EventInvalidated];
			if (invalidateEventHandler != null)
			{
				invalidateEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.KeyDown" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.KeyEventArgs" />, содержащий данные, относящиеся к событию. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnKeyDown(KeyEventArgs e)
		{
			KeyEventHandler keyEventHandler = (KeyEventHandler)base.Events[Control.EventKeyDown];
			if (keyEventHandler != null)
			{
				keyEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.KeyPress" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.KeyPressEventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnKeyPress(KeyPressEventArgs e)
		{
			KeyPressEventHandler keyPressEventHandler = (KeyPressEventHandler)base.Events[Control.EventKeyPress];
			if (keyPressEventHandler != null)
			{
				keyPressEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.KeyUp" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.KeyEventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnKeyUp(KeyEventArgs e)
		{
			KeyEventHandler keyEventHandler = (KeyEventHandler)base.Events[Control.EventKeyUp];
			if (keyEventHandler != null)
			{
				keyEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.Layout" />.</summary>
		/// <param name="levent">Объект <see cref="T:System.Windows.Forms.LayoutEventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnLayout(LayoutEventArgs levent)
		{
			if (this.IsActiveX)
			{
				this.ActiveXViewChanged();
			}
			LayoutEventHandler layoutEventHandler = (LayoutEventHandler)base.Events[Control.EventLayout];
			if (layoutEventHandler != null)
			{
				layoutEventHandler(this, levent);
			}
			if (this.LayoutEngine.Layout(this, levent) && this.ParentInternal != null)
			{
				this.ParentInternal.SetState(8388608, true);
			}
		}

		internal virtual void OnLayoutResuming(bool performLayout)
		{
			if (this.ParentInternal != null)
			{
				this.ParentInternal.OnChildLayoutResuming(this, performLayout);
			}
		}

		internal virtual void OnLayoutSuspended()
		{
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.Leave" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnLeave(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventLeave];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.LostFocus" /> для указанного элемента управления.</summary>
		/// <param name="toInvoke">Объект <see cref="T:System.Windows.Forms.Control" />, которому следует назначить событие. </param>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void InvokeLostFocus(Control toInvoke, EventArgs e)
		{
			if (toInvoke != null)
			{
				toInvoke.OnLostFocus(e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.LostFocus" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnLostFocus(EventArgs e)
		{
			if (this.IsActiveX)
			{
				this.ActiveXOnFocus(false);
			}
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventLostFocus];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.MarginChanged" />. </summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные, относящиеся к событию.</param>
		protected virtual void OnMarginChanged(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventMarginChanged];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.MouseDoubleClick" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.MouseEventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnMouseDoubleClick(MouseEventArgs e)
		{
			MouseEventHandler mouseEventHandler = (MouseEventHandler)base.Events[Control.EventMouseDoubleClick];
			if (mouseEventHandler != null)
			{
				mouseEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.MouseClick" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.MouseEventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnMouseClick(MouseEventArgs e)
		{
			MouseEventHandler mouseEventHandler = (MouseEventHandler)base.Events[Control.EventMouseClick];
			if (mouseEventHandler != null)
			{
				mouseEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.MouseCaptureChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnMouseCaptureChanged(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventMouseCaptureChanged];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.MouseDown" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.MouseEventArgs" />, содержащий данные, относящиеся к событию. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnMouseDown(MouseEventArgs e)
		{
			MouseEventHandler mouseEventHandler = (MouseEventHandler)base.Events[Control.EventMouseDown];
			if (mouseEventHandler != null)
			{
				mouseEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.MouseEnter" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnMouseEnter(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventMouseEnter];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.MouseLeave" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnMouseLeave(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventMouseLeave];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.MouseHover" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnMouseHover(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventMouseHover];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.MouseMove" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.MouseEventArgs" />, содержащий данные, относящиеся к событию. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnMouseMove(MouseEventArgs e)
		{
			MouseEventHandler mouseEventHandler = (MouseEventHandler)base.Events[Control.EventMouseMove];
			if (mouseEventHandler != null)
			{
				mouseEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.MouseUp" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.MouseEventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnMouseUp(MouseEventArgs e)
		{
			MouseEventHandler mouseEventHandler = (MouseEventHandler)base.Events[Control.EventMouseUp];
			if (mouseEventHandler != null)
			{
				mouseEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.MouseWheel" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.MouseEventArgs" />, содержащий данные, относящиеся к событию. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnMouseWheel(MouseEventArgs e)
		{
			MouseEventHandler mouseEventHandler = (MouseEventHandler)base.Events[Control.EventMouseWheel];
			if (mouseEventHandler != null)
			{
				mouseEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.Move" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnMove(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventMove];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			if (this.RenderTransparent)
			{
				this.Invalidate();
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.Paint" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.PaintEventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnPaint(PaintEventArgs e)
		{
			PaintEventHandler paintEventHandler = (PaintEventHandler)base.Events[Control.EventPaint];
			if (paintEventHandler != null)
			{
				paintEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.PaddingChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные, относящиеся к событию.</param>
		protected virtual void OnPaddingChanged(EventArgs e)
		{
			if (this.GetStyle(ControlStyles.ResizeRedraw))
			{
				this.Invalidate();
			}
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventPaddingChanged];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Рисует фон элемента управления.</summary>
		/// <param name="pevent">
		///   <see cref="T:System.Windows.Forms.PaintEventArgs" />, содержащий сведения об элементе управления, который следует нарисовать. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnPaintBackground(PaintEventArgs pevent)
		{
			NativeMethods.RECT rECT = default(NativeMethods.RECT);
			UnsafeNativeMethods.GetClientRect(new HandleRef(this.window, this.InternalHandle), ref rECT);
			this.PaintBackground(pevent, new Rectangle(rECT.left, rECT.top, rECT.right, rECT.bottom));
		}

		private void OnParentInvalidated(InvalidateEventArgs e)
		{
			if (!this.RenderTransparent)
			{
				return;
			}
			if (this.IsHandleCreated)
			{
				Rectangle rectangle = e.InvalidRect;
				Point location = this.Location;
				rectangle.Offset(-location.X, -location.Y);
				rectangle = Rectangle.Intersect(this.ClientRectangle, rectangle);
				if (rectangle.IsEmpty)
				{
					return;
				}
				this.Invalidate(rectangle);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.QueryContinueDrag" />.</summary>
		/// <param name="qcdevent">Объект <see cref="T:System.Windows.Forms.QueryContinueDragEventArgs" />, содержащий данные, относящиеся к событию. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnQueryContinueDrag(QueryContinueDragEventArgs qcdevent)
		{
			QueryContinueDragEventHandler queryContinueDragEventHandler = (QueryContinueDragEventHandler)base.Events[Control.EventQueryContinueDrag];
			if (queryContinueDragEventHandler != null)
			{
				queryContinueDragEventHandler(this, qcdevent);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.RegionChanged" />. </summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события.</param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnRegionChanged(EventArgs e)
		{
			EventHandler eventHandler = base.Events[Control.EventRegionChanged] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.Resize" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnResize(EventArgs e)
		{
			if ((this.controlStyle & ControlStyles.ResizeRedraw) == ControlStyles.ResizeRedraw || this.GetState(4194304))
			{
				this.Invalidate();
			}
			LayoutTransaction.DoLayout(this, this, PropertyNames.Bounds);
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventResize];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.PreviewKeyDown" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.PreviewKeyDownEventArgs" />, содержащий данные, относящиеся к событию.</param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode), SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		protected virtual void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
		{
			PreviewKeyDownEventHandler previewKeyDownEventHandler = (PreviewKeyDownEventHandler)base.Events[Control.EventPreviewKeyDown];
			if (previewKeyDownEventHandler != null)
			{
				previewKeyDownEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.SizeChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnSizeChanged(EventArgs e)
		{
			this.OnResize(EventArgs.Empty);
			EventHandler eventHandler = base.Events[Control.EventSize] as EventHandler;
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.ChangeUICues" />.</summary>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.UICuesEventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnChangeUICues(UICuesEventArgs e)
		{
			UICuesEventHandler uICuesEventHandler = (UICuesEventHandler)base.Events[Control.EventChangeUICues];
			if (uICuesEventHandler != null)
			{
				uICuesEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.StyleChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnStyleChanged(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventStyleChanged];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.SystemColorsChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnSystemColorsChanged(EventArgs e)
		{
			Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
			if (controlCollection != null)
			{
				for (int i = 0; i < controlCollection.Count; i++)
				{
					controlCollection[i].OnSystemColorsChanged(EventArgs.Empty);
				}
			}
			this.Invalidate();
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventSystemColorsChanged];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.Validating" />.</summary>
		/// <param name="e">Объект <see cref="T:System.ComponentModel.CancelEventArgs" />, содержащий данные, относящиеся к событию. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnValidating(CancelEventArgs e)
		{
			CancelEventHandler cancelEventHandler = (CancelEventHandler)base.Events[Control.EventValidating];
			if (cancelEventHandler != null)
			{
				cancelEventHandler(this, e);
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.Validated" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void OnValidated(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventValidated];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		internal void PaintBackground(PaintEventArgs e, Rectangle rectangle)
		{
			this.PaintBackground(e, rectangle, this.BackColor, Point.Empty);
		}

		internal void PaintBackground(PaintEventArgs e, Rectangle rectangle, Color backColor)
		{
			this.PaintBackground(e, rectangle, backColor, Point.Empty);
		}

		internal void PaintBackground(PaintEventArgs e, Rectangle rectangle, Color backColor, Point scrollOffset)
		{
			if (this.RenderColorTransparent(backColor))
			{
				this.PaintTransparentBackground(e, rectangle);
			}
			bool flag = (this is Form || this is MdiClient) && this.IsMirrored;
			if (this.BackgroundImage != null && !DisplayInformation.HighContrast && !flag)
			{
				if (this.BackgroundImageLayout == ImageLayout.Tile && ControlPaint.IsImageTransparent(this.BackgroundImage))
				{
					this.PaintTransparentBackground(e, rectangle);
				}
				Point point = scrollOffset;
				if (this is ScrollableControl && point != Point.Empty)
				{
					point = ((ScrollableControl)this).AutoScrollPosition;
				}
				if (ControlPaint.IsImageTransparent(this.BackgroundImage))
				{
					Control.PaintBackColor(e, rectangle, backColor);
				}
				ControlPaint.DrawBackgroundImage(e.Graphics, this.BackgroundImage, backColor, this.BackgroundImageLayout, this.ClientRectangle, rectangle, point, this.RightToLeft);
				return;
			}
			Control.PaintBackColor(e, rectangle, backColor);
		}

		private static void PaintBackColor(PaintEventArgs e, Rectangle rectangle, Color backColor)
		{
			Color color = backColor;
			if (color.A == 255)
			{
				using (WindowsGraphics windowsGraphics = (e.HDC != IntPtr.Zero && DisplayInformation.BitsPerPixel > 8) ? WindowsGraphics.FromHdc(e.HDC) : WindowsGraphics.FromGraphics(e.Graphics))
				{
					color = windowsGraphics.GetNearestColor(color);
					using (WindowsBrush windowsBrush = new WindowsSolidBrush(windowsGraphics.DeviceContext, color))
					{
						windowsGraphics.FillRectangle(windowsBrush, rectangle);
						return;
					}
				}
			}
			if (color.A > 0)
			{
				using (Brush brush = new SolidBrush(color))
				{
					e.Graphics.FillRectangle(brush, rectangle);
				}
			}
		}

		private void PaintException(PaintEventArgs e)
		{
			int num = 2;
			using (Pen pen = new Pen(Color.Red, (float)num))
			{
				Rectangle clientRectangle = this.ClientRectangle;
				Rectangle rect = clientRectangle;
				int num2 = rect.X;
				rect.X = num2 + 1;
				num2 = rect.Y;
				rect.Y = num2 + 1;
				num2 = rect.Width;
				rect.Width = num2 - 1;
				num2 = rect.Height;
				rect.Height = num2 - 1;
				e.Graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
				rect.Inflate(-1, -1);
				e.Graphics.FillRectangle(Brushes.White, rect);
				e.Graphics.DrawLine(pen, clientRectangle.Left, clientRectangle.Top, clientRectangle.Right, clientRectangle.Bottom);
				e.Graphics.DrawLine(pen, clientRectangle.Left, clientRectangle.Bottom, clientRectangle.Right, clientRectangle.Top);
			}
		}

		internal void PaintTransparentBackground(PaintEventArgs e, Rectangle rectangle)
		{
			this.PaintTransparentBackground(e, rectangle, null);
		}

		internal void PaintTransparentBackground(PaintEventArgs e, Rectangle rectangle, Region transparentRegion)
		{
			Graphics graphics = e.Graphics;
			Control parentInternal = this.ParentInternal;
			if (parentInternal != null)
			{
				if (Application.RenderWithVisualStyles && parentInternal.RenderTransparencyWithVisualStyles)
				{
					GraphicsState graphicsState = null;
					if (transparentRegion != null)
					{
						graphicsState = graphics.Save();
					}
					try
					{
						if (transparentRegion != null)
						{
							graphics.Clip = transparentRegion;
						}
						ButtonRenderer.DrawParentBackground(graphics, rectangle, this);
						return;
					}
					finally
					{
						if (graphicsState != null)
						{
							graphics.Restore(graphicsState);
						}
					}
				}
				Rectangle rectangle2 = new Rectangle(-this.Left, -this.Top, parentInternal.Width, parentInternal.Height);
				Rectangle clipRect = new Rectangle(rectangle.Left + this.Left, rectangle.Top + this.Top, rectangle.Width, rectangle.Height);
				using (WindowsGraphics windowsGraphics = WindowsGraphics.FromGraphics(graphics))
				{
					windowsGraphics.DeviceContext.TranslateTransform(-this.Left, -this.Top);
					using (PaintEventArgs paintEventArgs = new PaintEventArgs(windowsGraphics.GetHdc(), clipRect))
					{
						if (transparentRegion != null)
						{
							paintEventArgs.Graphics.Clip = transparentRegion;
							paintEventArgs.Graphics.TranslateClip(-rectangle2.X, -rectangle2.Y);
						}
						try
						{
							this.InvokePaintBackground(parentInternal, paintEventArgs);
							this.InvokePaint(parentInternal, paintEventArgs);
							return;
						}
						finally
						{
							if (transparentRegion != null)
							{
								paintEventArgs.Graphics.TranslateClip(rectangle2.X, rectangle2.Y);
							}
						}
					}
				}
			}
			graphics.FillRectangle(SystemBrushes.Control, rectangle);
		}

		private void PaintWithErrorHandling(PaintEventArgs e, short layer)
		{
			try
			{
				this.CacheTextInternal = true;
				if (this.GetState(4194304))
				{
					if (layer == 1)
					{
						this.PaintException(e);
					}
				}
				else
				{
					bool flag = true;
					try
					{
						if (layer != 1)
						{
							if (layer == 2)
							{
								this.OnPaint(e);
							}
						}
						else if (!this.GetStyle(ControlStyles.Opaque))
						{
							this.OnPaintBackground(e);
						}
						flag = false;
					}
					finally
					{
						if (flag)
						{
							this.SetState(4194304, true);
							this.Invalidate();
						}
					}
				}
			}
			finally
			{
				this.CacheTextInternal = false;
			}
		}

		/// <summary>Вызывает в элементе управления принудительное применение логики макета ко всем его дочерним элементам управления.</summary>
		/// <filterpriority>2</filterpriority>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public void PerformLayout()
		{
			if (this.cachedLayoutEventArgs != null)
			{
				this.PerformLayout(this.cachedLayoutEventArgs);
				this.cachedLayoutEventArgs = null;
				this.SetState2(64, false);
				return;
			}
			this.PerformLayout(null, null);
		}

		/// <summary>Вызывает в элементе управления принудительное применение логики макета ко всем его дочерним элементам управления.</summary>
		/// <param name="affectedControl">Объект <see cref="T:System.Windows.Forms.Control" />, представляющий последний измененный элемент управления. </param>
		/// <param name="affectedProperty">Имя последнего измененного свойства элемента управления. </param>
		/// <filterpriority>2</filterpriority>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public void PerformLayout(Control affectedControl, string affectedProperty)
		{
			this.PerformLayout(new LayoutEventArgs(affectedControl, affectedProperty));
		}

		internal void PerformLayout(LayoutEventArgs args)
		{
			if (this.GetAnyDisposingInHierarchy())
			{
				return;
			}
			if (this.layoutSuspendCount > 0)
			{
				this.SetState(512, true);
				if (this.cachedLayoutEventArgs == null || (this.GetState2(64) && args != null))
				{
					this.cachedLayoutEventArgs = args;
					if (this.GetState2(64))
					{
						this.SetState2(64, false);
					}
				}
				this.LayoutEngine.ProcessSuspendedLayoutEventArgs(this, args);
				return;
			}
			this.layoutSuspendCount = 1;
			try
			{
				this.CacheTextInternal = true;
				this.OnLayout(args);
			}
			finally
			{
				this.CacheTextInternal = false;
				this.SetState(8389120, false);
				this.layoutSuspendCount = 0;
				if (this.ParentInternal != null && this.ParentInternal.GetState(8388608))
				{
					LayoutTransaction.DoLayout(this.ParentInternal, this, PropertyNames.PreferredSize);
				}
			}
		}

		internal bool PerformControlValidation(bool bulkValidation)
		{
			if (!this.CausesValidation)
			{
				return false;
			}
			if (this.NotifyValidating())
			{
				return true;
			}
			if (bulkValidation || NativeWindow.WndProcShouldBeDebuggable)
			{
				this.NotifyValidated();
			}
			else
			{
				try
				{
					this.NotifyValidated();
				}
				catch (Exception arg_2F_0)
				{
					Application.OnThreadException(arg_2F_0);
				}
			}
			return false;
		}

		internal bool PerformContainerValidation(ValidationConstraints validationConstraints)
		{
			bool result = false;
			foreach (Control control in this.Controls)
			{
				if ((validationConstraints & ValidationConstraints.ImmediateChildren) != ValidationConstraints.ImmediateChildren && control.ShouldPerformContainerValidation() && control.PerformContainerValidation(validationConstraints))
				{
					result = true;
				}
				if (((validationConstraints & ValidationConstraints.Selectable) != ValidationConstraints.Selectable || control.GetStyle(ControlStyles.Selectable)) && ((validationConstraints & ValidationConstraints.Enabled) != ValidationConstraints.Enabled || control.Enabled) && ((validationConstraints & ValidationConstraints.Visible) != ValidationConstraints.Visible || control.Visible) && ((validationConstraints & ValidationConstraints.TabStop) != ValidationConstraints.TabStop || control.TabStop) && control.PerformControlValidation(true))
				{
					result = true;
				}
			}
			return result;
		}

		/// <summary>Вычисляет местоположение указанной точки экрана в клиентских координатах.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Point" />, представляющий преобразованный объект <see cref="T:System.Drawing.Point" />, <paramref name="p" />, в клиентских координатах.</returns>
		/// <param name="p">Преобразуемые <see cref="T:System.Drawing.Point" /> экранные координаты. </param>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public Point PointToClient(Point p)
		{
			return this.PointToClientInternal(p);
		}

		internal Point PointToClientInternal(Point p)
		{
			NativeMethods.POINT pOINT = new NativeMethods.POINT(p.X, p.Y);
			UnsafeNativeMethods.MapWindowPoints(NativeMethods.NullHandleRef, new HandleRef(this, this.Handle), pOINT, 1);
			return new Point(pOINT.x, pOINT.y);
		}

		/// <summary>Вычисляет местоположение указанной точки клиента в экранных координатах.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Point" />, представляющий преобразованный объект <see cref="T:System.Drawing.Point" />, <paramref name="p" />, в экранных координатах.</returns>
		/// <param name="p">Преобразуемый объект <see cref="T:System.Drawing.Point" /> клиентских координат. </param>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public Point PointToScreen(Point p)
		{
			NativeMethods.POINT pOINT = new NativeMethods.POINT(p.X, p.Y);
			UnsafeNativeMethods.MapWindowPoints(new HandleRef(this, this.Handle), NativeMethods.NullHandleRef, pOINT, 1);
			return new Point(pOINT.x, pOINT.y);
		}

		/// <summary>Выполняет предварительную обработку клавиатурных или входящих сообщений в цикле обработки сообщений перед их отправкой.</summary>
		/// <returns>Значение true, если сообщение было обработано элементом управления; в противном случае — значение false.</returns>
		/// <param name="msg">Переданный по ссылке объект <see cref="T:System.Windows.Forms.Message" />, представляющий обрабатываемое сообщение.Возможными значениями являются WM_KEYDOWN, WM_SYSKEYDOWN, WM_CHAR и WM_SYSCHAR.</param>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode), SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public virtual bool PreProcessMessage(ref Message msg)
		{
			bool result;
			if (msg.Msg == 256 || msg.Msg == 260)
			{
				if (!this.GetState2(512))
				{
					this.ProcessUICues(ref msg);
				}
				Keys keyData = (Keys)((long)msg.WParam) | Control.ModifierKeys;
				if (this.ProcessCmdKey(ref msg, keyData))
				{
					result = true;
					return result;
				}
				if (this.IsInputKey(keyData))
				{
					this.SetState2(128, true);
					result = false;
					return result;
				}
				IntSecurity.ModifyFocus.Assert();
				try
				{
					result = this.ProcessDialogKey(keyData);
					return result;
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
				}
			}
			if (msg.Msg == 258 || msg.Msg == 262)
			{
				if (msg.Msg == 258 && this.IsInputChar((char)((int)msg.WParam)))
				{
					this.SetState2(256, true);
					result = false;
				}
				else
				{
					result = this.ProcessDialogChar((char)((int)msg.WParam));
				}
			}
			else
			{
				result = false;
			}
			return result;
		}

		/// <summary>Выполняет предварительную обработку клавиатурных или входящих сообщений в цикле обработки сообщений перед их отправкой.</summary>
		/// <returns>Одно из значений <see cref="T:System.Windows.Forms.PreProcessControlState" />, зависящее от того, какое значение имеет метод <see cref="M:System.Windows.Forms.Control.PreProcessMessage(System.Windows.Forms.Message@)" /> —true или false, а также какое значение имеет метод <see cref="M:System.Windows.Forms.Control.IsInputKey(System.Windows.Forms.Keys)" /> или <see cref="M:System.Windows.Forms.Control.IsInputChar(System.Char)" /> — true или false.</returns>
		/// <param name="msg">Объект <see cref="T:System.Windows.Forms.Message" />, представляющий сообщение, которое требуется обработать.</param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public PreProcessControlState PreProcessControlMessage(ref Message msg)
		{
			return Control.PreProcessControlMessageInternal(null, ref msg);
		}

		internal static PreProcessControlState PreProcessControlMessageInternal(Control target, ref Message msg)
		{
			if (target == null)
			{
				target = Control.FromChildHandleInternal(msg.HWnd);
			}
			if (target == null)
			{
				return PreProcessControlState.MessageNotNeeded;
			}
			target.SetState2(128, false);
			target.SetState2(256, false);
			target.SetState2(512, true);
			PreProcessControlState result;
			try
			{
				Keys keyData = (Keys)((long)msg.WParam) | Control.ModifierKeys;
				if (msg.Msg == 256 || msg.Msg == 260)
				{
					target.ProcessUICues(ref msg);
					PreviewKeyDownEventArgs previewKeyDownEventArgs = new PreviewKeyDownEventArgs(keyData);
					target.OnPreviewKeyDown(previewKeyDownEventArgs);
					if (previewKeyDownEventArgs.IsInputKey)
					{
						result = PreProcessControlState.MessageNeeded;
						return result;
					}
				}
				PreProcessControlState preProcessControlState = PreProcessControlState.MessageNotNeeded;
				if (!target.PreProcessMessage(ref msg))
				{
					if (msg.Msg == 256 || msg.Msg == 260)
					{
						if (target.GetState2(128) || target.IsInputKey(keyData))
						{
							preProcessControlState = PreProcessControlState.MessageNeeded;
						}
					}
					else if ((msg.Msg == 258 || msg.Msg == 262) && (target.GetState2(256) || target.IsInputChar((char)((int)msg.WParam))))
					{
						preProcessControlState = PreProcessControlState.MessageNeeded;
					}
				}
				else
				{
					preProcessControlState = PreProcessControlState.MessageProcessed;
				}
				result = preProcessControlState;
			}
			finally
			{
				target.SetState2(512, false);
			}
			return result;
		}

		/// <summary>Обрабатывает управляющую клавишу.</summary>
		/// <returns>Значение true, если символ был обработан элементом управления; в противном случае — значение false.</returns>
		/// <param name="msg">Передаваемый по ссылке объект <see cref="T:System.Windows.Forms.Message" />, представляющий сообщение окна для обработки. </param>
		/// <param name="keyData">Одно из значений <see cref="T:System.Windows.Forms.Keys" />, представляющее обрабатываемую клавишу. </param>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode), SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		protected virtual bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			ContextMenu contextMenu = (ContextMenu)this.Properties.GetObject(Control.PropContextMenu);
			return (contextMenu != null && contextMenu.ProcessCmdKey(ref msg, keyData, this)) || (this.parent != null && this.parent.ProcessCmdKey(ref msg, keyData));
		}

		private void PrintToMetaFile(HandleRef hDC, IntPtr lParam)
		{
			lParam = (IntPtr)((long)lParam & -17L);
			NativeMethods.POINT pOINT = new NativeMethods.POINT();
			SafeNativeMethods.GetViewportOrgEx(hDC, pOINT);
			HandleRef handleRef = new HandleRef(null, SafeNativeMethods.CreateRectRgn(pOINT.x, pOINT.y, pOINT.x + this.Width, pOINT.y + this.Height));
			try
			{
				SafeNativeMethods.SelectClipRgn(hDC, handleRef);
				this.PrintToMetaFileRecursive(hDC, lParam, new Rectangle(Point.Empty, this.Size));
			}
			finally
			{
				SafeNativeMethods.DeleteObject(handleRef);
			}
		}

		internal virtual void PrintToMetaFileRecursive(HandleRef hDC, IntPtr lParam, Rectangle bounds)
		{
			using (new WindowsFormsUtils.DCMapping(hDC, bounds))
			{
				this.PrintToMetaFile_SendPrintMessage(hDC, (IntPtr)((long)lParam & -5L));
				NativeMethods.RECT rECT = default(NativeMethods.RECT);
				UnsafeNativeMethods.GetWindowRect(new HandleRef(null, this.Handle), ref rECT);
				Point location = this.PointToScreen(Point.Empty);
				location = new Point(location.X - rECT.left, location.Y - rECT.top);
				Rectangle bounds2 = new Rectangle(location, this.ClientSize);
				using (new WindowsFormsUtils.DCMapping(hDC, bounds2))
				{
					this.PrintToMetaFile_SendPrintMessage(hDC, (IntPtr)((long)lParam & -3L));
					for (int i = this.Controls.Count - 1; i >= 0; i--)
					{
						Control control = this.Controls[i];
						if (control.Visible)
						{
							control.PrintToMetaFileRecursive(hDC, lParam, control.Bounds);
						}
					}
				}
			}
		}

		private void PrintToMetaFile_SendPrintMessage(HandleRef hDC, IntPtr lParam)
		{
			if (this.GetStyle(ControlStyles.UserPaint))
			{
				this.SendMessage(791, hDC.Handle, lParam);
				return;
			}
			if (this.Controls.Count == 0)
			{
				lParam = (IntPtr)((long)lParam | 16L);
			}
			using (Control.MetafileDCWrapper metafileDCWrapper = new Control.MetafileDCWrapper(hDC, this.Size))
			{
				this.SendMessage(791, metafileDCWrapper.HDC, lParam);
			}
		}

		/// <summary>Обрабатывает символ диалогового окна.</summary>
		/// <returns>Значение true, если символ был обработан элементом управления; в противном случае — значение false.</returns>
		/// <param name="charCode">Символ, подлежащий обработке. </param>
		[UIPermission(SecurityAction.LinkDemand, Window = UIPermissionWindow.AllWindows), UIPermission(SecurityAction.InheritanceDemand, Window = UIPermissionWindow.AllWindows)]
		protected virtual bool ProcessDialogChar(char charCode)
		{
			return this.parent != null && this.parent.ProcessDialogChar(charCode);
		}

		/// <summary>Обрабатывает клавишу диалогового окна.</summary>
		/// <returns>Значение true, если клавиша была обработана элементом управления; в противном случае — значение false.</returns>
		/// <param name="keyData">Одно из значений <see cref="T:System.Windows.Forms.Keys" />, представляющее обрабатываемую клавишу. </param>
		[UIPermission(SecurityAction.LinkDemand, Window = UIPermissionWindow.AllWindows), UIPermission(SecurityAction.InheritanceDemand, Window = UIPermissionWindow.AllWindows)]
		protected virtual bool ProcessDialogKey(Keys keyData)
		{
			return this.parent != null && this.parent.ProcessDialogKey(keyData);
		}

		/// <summary>Обрабатывает сообщение о нажатии клавиши и создает соответствующие события элемента управления.</summary>
		/// <returns>Значение true, если сообщение было обработано элементом управления; в противном случае — значение false.</returns>
		/// <param name="m">Передаваемый по ссылке объект <see cref="T:System.Windows.Forms.Message" />, представляющий сообщение окна для обработки. </param>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode), SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		protected virtual bool ProcessKeyEventArgs(ref Message m)
		{
			KeyEventArgs keyEventArgs = null;
			KeyPressEventArgs keyPressEventArgs = null;
			IntPtr wParam = IntPtr.Zero;
			if (m.Msg == 258 || m.Msg == 262)
			{
				int num = this.ImeWmCharsToIgnore;
				if (num > 0)
				{
					num--;
					this.ImeWmCharsToIgnore = num;
					return false;
				}
				keyPressEventArgs = new KeyPressEventArgs((char)((long)m.WParam));
				this.OnKeyPress(keyPressEventArgs);
				wParam = (IntPtr)((int)keyPressEventArgs.KeyChar);
			}
			else if (m.Msg == 646)
			{
				int num2 = this.ImeWmCharsToIgnore;
				if (Marshal.SystemDefaultCharSize == 1)
				{
					char keyChar = '\0';
					byte[] array = new byte[]
					{
						(byte)((int)((long)m.WParam) >> 8),
						(byte)((long)m.WParam)
					};
					char[] array2 = new char[1];
					int arg_C7_0 = 0;
					int arg_C7_1 = 1;
					byte[] expr_C1 = array;
					int num3 = UnsafeNativeMethods.MultiByteToWideChar(arg_C7_0, arg_C7_1, expr_C1, expr_C1.Length, array2, 0);
					if (num3 <= 0)
					{
						throw new Win32Exception();
					}
					array2 = new char[num3];
					int arg_E8_0 = 0;
					int arg_E8_1 = 1;
					byte[] expr_E0 = array;
					int arg_E8_3 = expr_E0.Length;
					char[] expr_E5 = array2;
					UnsafeNativeMethods.MultiByteToWideChar(arg_E8_0, arg_E8_1, expr_E0, arg_E8_3, expr_E5, expr_E5.Length);
					if (array2[0] != '\0')
					{
						keyChar = array2[0];
						num2 += 2;
					}
					else if (array2[0] == '\0' && array2.Length >= 2)
					{
						keyChar = array2[1];
						num2++;
					}
					this.ImeWmCharsToIgnore = num2;
					keyPressEventArgs = new KeyPressEventArgs(keyChar);
				}
				else
				{
					num2 += 3 - Marshal.SystemDefaultCharSize;
					this.ImeWmCharsToIgnore = num2;
					keyPressEventArgs = new KeyPressEventArgs((char)((long)m.WParam));
				}
				char keyChar2 = keyPressEventArgs.KeyChar;
				this.OnKeyPress(keyPressEventArgs);
				if (keyPressEventArgs.KeyChar == keyChar2)
				{
					wParam = m.WParam;
				}
				else if (Marshal.SystemDefaultCharSize == 1)
				{
					string text = new string(new char[]
					{
						keyPressEventArgs.KeyChar
					});
					int arg_1BA_0 = 0;
					int arg_1BA_1 = 0;
					string expr_1A8 = text;
					int num4 = UnsafeNativeMethods.WideCharToMultiByte(arg_1BA_0, arg_1BA_1, expr_1A8, expr_1A8.Length, null, 0, IntPtr.Zero, IntPtr.Zero);
					if (num4 >= 2)
					{
						byte[] array3 = new byte[num4];
						int arg_1E8_0 = 0;
						int arg_1E8_1 = 0;
						string expr_1D3 = text;
						int arg_1E8_3 = expr_1D3.Length;
						byte[] expr_1DB = array3;
						UnsafeNativeMethods.WideCharToMultiByte(arg_1E8_0, arg_1E8_1, expr_1D3, arg_1E8_3, expr_1DB, expr_1DB.Length, IntPtr.Zero, IntPtr.Zero);
						int num5 = Marshal.SizeOf(typeof(IntPtr));
						if (num4 > num5)
						{
							num4 = num5;
						}
						long num6 = 0L;
						for (int i = 0; i < num4; i++)
						{
							num6 <<= 8;
							num6 |= (long)((ulong)array3[i]);
						}
						wParam = (IntPtr)num6;
					}
					else if (num4 == 1)
					{
						byte[] array3 = new byte[num4];
						int arg_263_0 = 0;
						int arg_263_1 = 0;
						string expr_24E = text;
						int arg_263_3 = expr_24E.Length;
						byte[] expr_256 = array3;
						UnsafeNativeMethods.WideCharToMultiByte(arg_263_0, arg_263_1, expr_24E, arg_263_3, expr_256, expr_256.Length, IntPtr.Zero, IntPtr.Zero);
						wParam = (IntPtr)((int)array3[0]);
					}
					else
					{
						wParam = m.WParam;
					}
				}
				else
				{
					wParam = (IntPtr)((int)keyPressEventArgs.KeyChar);
				}
			}
			else
			{
				keyEventArgs = new KeyEventArgs((Keys)((long)m.WParam) | Control.ModifierKeys);
				if (m.Msg == 256 || m.Msg == 260)
				{
					this.OnKeyDown(keyEventArgs);
				}
				else
				{
					this.OnKeyUp(keyEventArgs);
				}
			}
			if (keyPressEventArgs != null)
			{
				m.WParam = wParam;
				return keyPressEventArgs.Handled;
			}
			if (keyEventArgs.SuppressKeyPress)
			{
				this.RemovePendingMessages(258, 258);
				this.RemovePendingMessages(262, 262);
				this.RemovePendingMessages(646, 646);
			}
			return keyEventArgs.Handled;
		}

		/// <summary>Обрабатывает сообщение клавиатуры.</summary>
		/// <returns>Значение true, если сообщение было обработано элементом управления; в противном случае — значение false.</returns>
		/// <param name="m">Передаваемый по ссылке объект <see cref="T:System.Windows.Forms.Message" />, представляющий сообщение окна для обработки. </param>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode), SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		protected internal virtual bool ProcessKeyMessage(ref Message m)
		{
			return (this.parent != null && this.parent.ProcessKeyPreview(ref m)) || this.ProcessKeyEventArgs(ref m);
		}

		/// <summary>Выполняет предварительный просмотр сообщения клавиатуры.</summary>
		/// <returns>Значение true, если сообщение было обработано элементом управления; в противном случае — значение false.</returns>
		/// <param name="m">Передаваемый по ссылке объект <see cref="T:System.Windows.Forms.Message" />, представляющий сообщение окна для обработки. </param>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode), SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		protected virtual bool ProcessKeyPreview(ref Message m)
		{
			return this.parent != null && this.parent.ProcessKeyPreview(ref m);
		}

		/// <summary>Обрабатывает назначенный символ.</summary>
		/// <returns>Значение true, если символ был обработан элементом управления как назначенный. В противном случае — значение false.</returns>
		/// <param name="charCode">Символ, подлежащий обработке. </param>
		[UIPermission(SecurityAction.LinkDemand, Window = UIPermissionWindow.AllWindows), UIPermission(SecurityAction.InheritanceDemand, Window = UIPermissionWindow.AllWindows)]
		protected internal virtual bool ProcessMnemonic(char charCode)
		{
			return false;
		}

		internal void ProcessUICues(ref Message msg)
		{
			Keys keys = (Keys)((int)msg.WParam & 65535);
			if (keys != Keys.F10 && keys != Keys.Menu && keys != Keys.Tab)
			{
				return;
			}
			Control control = null;
			int num = (int)((long)this.SendMessage(297, 0, 0));
			if (num == 0)
			{
				control = this.TopMostParent;
				num = (int)control.SendMessage(297, 0, 0);
			}
			int num2 = 0;
			if ((keys == Keys.F10 || keys == Keys.Menu) && (num & 2) != 0)
			{
				num2 |= 2;
			}
			if (keys == Keys.Tab && (num & 1) != 0)
			{
				num2 |= 1;
			}
			if (num2 != 0)
			{
				if (control == null)
				{
					control = this.TopMostParent;
				}
				UnsafeNativeMethods.SendMessage(new HandleRef(control, control.Handle), (UnsafeNativeMethods.GetParent(new HandleRef(null, control.Handle)) == IntPtr.Zero) ? 295 : 296, (IntPtr)(2 | num2 << 16), IntPtr.Zero);
			}
		}

		/// <summary>Вызывает соответствующее событие перетаскивания.</summary>
		/// <param name="key">Вызываемое событие. </param>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.DragEventArgs" />, содержащий данные, относящиеся к событию. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void RaiseDragEvent(object key, DragEventArgs e)
		{
			DragEventHandler dragEventHandler = (DragEventHandler)base.Events[key];
			if (dragEventHandler != null)
			{
				dragEventHandler(this, e);
			}
		}

		/// <summary>Вызывает соответствующее событие окрашивания.</summary>
		/// <param name="key">Вызываемое событие. </param>
		/// <param name="e">Объект <see cref="T:System.Windows.Forms.PaintEventArgs" />, содержащий данные, относящиеся к событию. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void RaisePaintEvent(object key, PaintEventArgs e)
		{
			PaintEventHandler paintEventHandler = (PaintEventHandler)base.Events[Control.EventPaint];
			if (paintEventHandler != null)
			{
				paintEventHandler(this, e);
			}
		}

		private void RemovePendingMessages(int msgMin, int msgMax)
		{
			if (!this.IsDisposed)
			{
				NativeMethods.MSG mSG = default(NativeMethods.MSG);
				IntPtr handle = this.Handle;
				while (UnsafeNativeMethods.PeekMessage(ref mSG, new HandleRef(this, handle), msgMin, msgMax, 1))
				{
				}
			}
		}

		/// <summary>Сбрасывает свойство <see cref="P:System.Windows.Forms.Control.BackColor" /> в значение по умолчанию.</summary>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual void ResetBackColor()
		{
			this.BackColor = Color.Empty;
		}

		/// <summary>Сбрасывает свойство <see cref="P:System.Windows.Forms.Control.Cursor" /> в значение по умолчанию.</summary>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual void ResetCursor()
		{
			this.Cursor = null;
		}

		private void ResetEnabled()
		{
			this.Enabled = true;
		}

		/// <summary>Сбрасывает свойство <see cref="P:System.Windows.Forms.Control.Font" /> в значение по умолчанию.</summary>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual void ResetFont()
		{
			this.Font = null;
		}

		/// <summary>Сбрасывает свойство <see cref="P:System.Windows.Forms.Control.ForeColor" /> в значение по умолчанию.</summary>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual void ResetForeColor()
		{
			this.ForeColor = Color.Empty;
		}

		private void ResetLocation()
		{
			this.Location = new Point(0, 0);
		}

		private void ResetMargin()
		{
			this.Margin = this.DefaultMargin;
		}

		private void ResetMinimumSize()
		{
			this.MinimumSize = this.DefaultMinimumSize;
		}

		private void ResetPadding()
		{
			CommonProperties.ResetPadding(this);
		}

		private void ResetSize()
		{
			this.Size = this.DefaultSize;
		}

		/// <summary>Сбрасывает свойство <see cref="P:System.Windows.Forms.Control.RightToLeft" /> в значение по умолчанию.</summary>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual void ResetRightToLeft()
		{
			this.RightToLeft = RightToLeft.Inherit;
		}

		/// <summary>Вызывает повторное создание дескриптора элемента управления.</summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void RecreateHandle()
		{
			this.RecreateHandleCore();
		}

		internal virtual void RecreateHandleCore()
		{
			lock (this)
			{
				if (this.IsHandleCreated)
				{
					bool containsFocus = this.ContainsFocus;
					bool flag2 = (this.state & 1) != 0;
					if (this.GetState(16384))
					{
						this.SetState(8192, true);
						this.UnhookMouseEvent();
					}
					HandleRef handleRef = new HandleRef(this, UnsafeNativeMethods.GetParent(new HandleRef(this, this.Handle)));
					try
					{
						Control[] array = null;
						this.state |= 16;
						try
						{
							Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
							if (controlCollection != null && controlCollection.Count > 0)
							{
								array = new Control[controlCollection.Count];
								for (int i = 0; i < controlCollection.Count; i++)
								{
									Control control = controlCollection[i];
									if (control != null && control.IsHandleCreated)
									{
										control.OnParentHandleRecreating();
										array[i] = control;
									}
									else
									{
										array[i] = null;
									}
								}
							}
							this.DestroyHandle();
							this.CreateHandle();
						}
						finally
						{
							this.state &= -17;
							if (array != null)
							{
								for (int j = 0; j < array.Length; j++)
								{
									Control control2 = array[j];
									if (control2 != null && control2.IsHandleCreated)
									{
										control2.OnParentHandleRecreated();
									}
								}
							}
						}
						if (flag2)
						{
							this.CreateControl();
						}
					}
					finally
					{
						if (handleRef.Handle != IntPtr.Zero && (Control.FromHandleInternal(handleRef.Handle) == null || this.parent == null) && UnsafeNativeMethods.IsWindow(handleRef))
						{
							UnsafeNativeMethods.SetParent(new HandleRef(this, this.Handle), handleRef);
						}
					}
					if (containsFocus)
					{
						this.FocusInternal();
					}
				}
			}
		}

		/// <summary>Вычисляет размер и местоположение указанной прямоугольной области экрана в клиентских координатах.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Rectangle" />, представляющий преобразованный объект <see cref="T:System.Drawing.Rectangle" />, <paramref name="r" />, в клиентских координатах.</returns>
		/// <param name="r">Преобразуемые <see cref="T:System.Drawing.Rectangle" /> экранные координаты. </param>
		/// <filterpriority>2</filterpriority>
		public Rectangle RectangleToClient(Rectangle r)
		{
			NativeMethods.RECT rECT = NativeMethods.RECT.FromXYWH(r.X, r.Y, r.Width, r.Height);
			UnsafeNativeMethods.MapWindowPoints(NativeMethods.NullHandleRef, new HandleRef(this, this.Handle), ref rECT, 2);
			return Rectangle.FromLTRB(rECT.left, rECT.top, rECT.right, rECT.bottom);
		}

		/// <summary>Вычисляет размер и местоположение указанной клиентской области (в виде прямоугольника) в экранных координатах.</summary>
		/// <returns>Объект <see cref="T:System.Drawing.Rectangle" />, представляющий преобразованный объект <see cref="T:System.Drawing.Rectangle" />, <paramref name="p" />, в экранных координатах.</returns>
		/// <param name="r">Преобразуемый объект <see cref="T:System.Drawing.Rectangle" /> клиентских координат. </param>
		/// <filterpriority>2</filterpriority>
		public Rectangle RectangleToScreen(Rectangle r)
		{
			NativeMethods.RECT rECT = NativeMethods.RECT.FromXYWH(r.X, r.Y, r.Width, r.Height);
			UnsafeNativeMethods.MapWindowPoints(new HandleRef(this, this.Handle), NativeMethods.NullHandleRef, ref rECT, 2);
			return Rectangle.FromLTRB(rECT.left, rECT.top, rECT.right, rECT.bottom);
		}

		/// <summary>Пересылает указанное сообщение элементу управления, связанному с заданным дескриптором.</summary>
		/// <returns>Значение true, если сообщение было переслано; в противном случае — значение false.</returns>
		/// <param name="hWnd">Объект <see cref="T:System.IntPtr" />, представляющий дескриптор элемента управления, которому следует переслать сообщение. </param>
		/// <param name="m">Объект <see cref="T:System.Windows.Forms.Message" />, предоставляющий пересылаемое сообщение Windows. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected static bool ReflectMessage(IntPtr hWnd, ref Message m)
		{
			IntSecurity.SendMessages.Demand();
			return Control.ReflectMessageInternal(hWnd, ref m);
		}

		internal static bool ReflectMessageInternal(IntPtr hWnd, ref Message m)
		{
			Control control = Control.FromHandleInternal(hWnd);
			if (control == null)
			{
				return false;
			}
			m.Result = control.SendMessage(8192 + m.Msg, m.WParam, m.LParam);
			return true;
		}

		/// <summary>Принудительно создает условия, при которых элемент управления делает недоступной свою клиентскую область и немедленно перерисовывает себя и все дочерние элементы.</summary>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public virtual void Refresh()
		{
			this.Invalidate(true);
			this.Update();
		}

		/// <summary>Сбрасывает элемент управления в дескриптор события <see cref="E:System.Windows.Forms.Control.MouseLeave" />.</summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void ResetMouseEventArgs()
		{
			if (this.GetState(16384))
			{
				this.UnhookMouseEvent();
				this.HookMouseEvent();
			}
		}

		/// <summary>Сбрасывает свойство <see cref="P:System.Windows.Forms.Control.Text" /> в значение по умолчанию.</summary>
		/// <filterpriority>2</filterpriority>
		public virtual void ResetText()
		{
			this.Text = string.Empty;
		}

		private void ResetVisible()
		{
			this.Visible = true;
		}

		/// <summary>Возобновляет обычную логику макета.</summary>
		/// <filterpriority>1</filterpriority>
		public void ResumeLayout()
		{
			this.ResumeLayout(true);
		}

		/// <summary>Возобновляет обычную логику макета, дополнительно осуществляя немедленное отображение отложенных запросов макета.</summary>
		/// <param name="performLayout">Значение true, чтобы выполнить отложенные запросы макета; в противном случае — значение false. </param>
		/// <filterpriority>1</filterpriority>
		public void ResumeLayout(bool performLayout)
		{
			bool flag = false;
			if (this.layoutSuspendCount > 0)
			{
				if (this.layoutSuspendCount == 1)
				{
					this.layoutSuspendCount += 1;
					try
					{
						this.OnLayoutResuming(performLayout);
					}
					finally
					{
						this.layoutSuspendCount -= 1;
					}
				}
				this.layoutSuspendCount -= 1;
				if ((this.layoutSuspendCount == 0 && this.GetState(512)) & performLayout)
				{
					this.PerformLayout();
					flag = true;
				}
			}
			if (!flag)
			{
				this.SetState2(64, true);
			}
			if (!performLayout)
			{
				CommonProperties.xClearPreferredSizeCache(this);
				Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
				if (controlCollection != null)
				{
					for (int i = 0; i < controlCollection.Count; i++)
					{
						this.LayoutEngine.InitLayout(controlCollection[i], BoundsSpecified.All);
						CommonProperties.xClearPreferredSizeCache(controlCollection[i]);
					}
				}
			}
		}

		internal void SetAcceptDrops(bool accept)
		{
			if (accept != this.GetState(128) && this.IsHandleCreated)
			{
				try
				{
					if (Application.OleRequired() != ApartmentState.STA)
					{
						throw new ThreadStateException(SR.GetString("ThreadMustBeSTA"));
					}
					if (accept)
					{
						IntSecurity.ClipboardRead.Demand();
						int num = UnsafeNativeMethods.RegisterDragDrop(new HandleRef(this, this.Handle), new DropTarget(this));
						if (num != 0 && num != -2147221247)
						{
							throw new Win32Exception(num);
						}
					}
					else
					{
						int num2 = UnsafeNativeMethods.RevokeDragDrop(new HandleRef(this, this.Handle));
						if (num2 != 0 && num2 != -2147221248)
						{
							throw new Win32Exception(num2);
						}
					}
					this.SetState(128, accept);
				}
				catch (Exception innerException)
				{
					throw new InvalidOperationException(SR.GetString("DragDropRegFailed"), innerException);
				}
			}
		}

		/// <summary>Масштабирует элемент управления и любые его дочерние элементы.</summary>
		/// <param name="ratio">Отношение, используемое для масштабирования.</param>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[EditorBrowsable(EditorBrowsableState.Never), Obsolete("This method has been deprecated. Use the Scale(SizeF ratio) method instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		public void Scale(float ratio)
		{
			this.ScaleCore(ratio, ratio);
		}

		/// <summary>Масштабирует весь элемент управления и любые его дочерние элементы.</summary>
		/// <param name="dx">Коэффициент горизонтального масштабирования.</param>
		/// <param name="dy">Коэффициент вертикального масштабирования.</param>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[EditorBrowsable(EditorBrowsableState.Never), Obsolete("This method has been deprecated. Use the Scale(SizeF ratio) method instead. http://go.microsoft.com/fwlink/?linkid=14202")]
		public void Scale(float dx, float dy)
		{
			this.SuspendLayout();
			try
			{
				this.ScaleCore(dx, dy);
			}
			finally
			{
				this.ResumeLayout();
			}
		}

		/// <summary>Масштабирует элемент управления и любые его дочерние элементы с использованием заданного коэффициента масштабирования.</summary>
		/// <param name="factor">Объект <see cref="T:System.Drawing.SizeF" />, содержащий коэффициенты вертикального и горизонтального масштабирования.</param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public void Scale(SizeF factor)
		{
			using (new LayoutTransaction(this, this, PropertyNames.Bounds, false))
			{
				this.ScaleControl(factor, factor, this);
				if (this.ScaleChildren)
				{
					Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
					if (controlCollection != null)
					{
						for (int i = 0; i < controlCollection.Count; i++)
						{
							controlCollection[i].Scale(factor);
						}
					}
				}
			}
			LayoutTransaction.DoLayout(this, this, PropertyNames.Bounds);
		}

		internal virtual void Scale(SizeF includedFactor, SizeF excludedFactor, Control requestingControl)
		{
			using (new LayoutTransaction(this, this, PropertyNames.Bounds, false))
			{
				this.ScaleControl(includedFactor, excludedFactor, requestingControl);
				this.ScaleChildControls(includedFactor, excludedFactor, requestingControl);
			}
			LayoutTransaction.DoLayout(this, this, PropertyNames.Bounds);
		}

		internal void ScaleChildControls(SizeF includedFactor, SizeF excludedFactor, Control requestingControl)
		{
			if (this.ScaleChildren)
			{
				Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
				if (controlCollection != null)
				{
					for (int i = 0; i < controlCollection.Count; i++)
					{
						controlCollection[i].Scale(includedFactor, excludedFactor, requestingControl);
					}
				}
			}
		}

		internal void ScaleControl(SizeF includedFactor, SizeF excludedFactor, Control requestingControl)
		{
			BoundsSpecified boundsSpecified = BoundsSpecified.None;
			BoundsSpecified boundsSpecified2 = BoundsSpecified.None;
			if (!includedFactor.IsEmpty)
			{
				boundsSpecified = this.RequiredScaling;
			}
			if (!excludedFactor.IsEmpty)
			{
				boundsSpecified2 |= (~this.RequiredScaling & BoundsSpecified.All);
			}
			if (boundsSpecified != BoundsSpecified.None)
			{
				this.ScaleControl(includedFactor, boundsSpecified);
			}
			if (boundsSpecified2 != BoundsSpecified.None)
			{
				this.ScaleControl(excludedFactor, boundsSpecified2);
			}
			if (!includedFactor.IsEmpty)
			{
				this.RequiredScaling = BoundsSpecified.None;
			}
		}

		/// <summary>Выполняет масштабирование расположения, размеров, заполнения и полей элемента управления.</summary>
		/// <param name="factor">Коэффициент масштабирования высоты и ширины элемента управления.</param>
		/// <param name="specified">Значение <see cref="T:System.Windows.Forms.BoundsSpecified" />, задающее границы элемента управления, используемые для определения его размеров и положения.</param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void ScaleControl(SizeF factor, BoundsSpecified specified)
		{
			CreateParams createParams = this.CreateParams;
			NativeMethods.RECT rECT = new NativeMethods.RECT(0, 0, 0, 0);
			SafeNativeMethods.AdjustWindowRectEx(ref rECT, createParams.Style, this.HasMenu, createParams.ExStyle);
			Size size = this.MinimumSize;
			Size size2 = this.MaximumSize;
			this.MinimumSize = Size.Empty;
			this.MaximumSize = Size.Empty;
			Rectangle scaledBounds = this.GetScaledBounds(this.Bounds, factor, specified);
			float num = factor.Width;
			float num2 = factor.Height;
			Padding padding = this.Padding;
			Padding margin = this.Margin;
			if (num == 1f)
			{
				specified &= ~(BoundsSpecified.X | BoundsSpecified.Width);
			}
			if (num2 == 1f)
			{
				specified &= ~(BoundsSpecified.Y | BoundsSpecified.Height);
			}
			if (num != 1f)
			{
				padding.Left = (int)Math.Round((double)((float)padding.Left * num));
				padding.Right = (int)Math.Round((double)((float)padding.Right * num));
				margin.Left = (int)Math.Round((double)((float)margin.Left * num));
				margin.Right = (int)Math.Round((double)((float)margin.Right * num));
			}
			if (num2 != 1f)
			{
				padding.Top = (int)Math.Round((double)((float)padding.Top * num2));
				padding.Bottom = (int)Math.Round((double)((float)padding.Bottom * num2));
				margin.Top = (int)Math.Round((double)((float)margin.Top * num2));
				margin.Bottom = (int)Math.Round((double)((float)margin.Bottom * num2));
			}
			this.Padding = padding;
			this.Margin = margin;
			Size size3 = rECT.Size;
			if (!size.IsEmpty)
			{
				size -= size3;
				size = this.ScaleSize(LayoutUtils.UnionSizes(Size.Empty, size), factor.Width, factor.Height) + size3;
			}
			if (!size2.IsEmpty)
			{
				size2 -= size3;
				size2 = this.ScaleSize(LayoutUtils.UnionSizes(Size.Empty, size2), factor.Width, factor.Height) + size3;
			}
			Size b = LayoutUtils.ConvertZeroToUnbounded(size2);
			Size a = LayoutUtils.IntersectSizes(scaledBounds.Size, b);
			a = LayoutUtils.UnionSizes(a, size);
			this.SetBoundsCore(scaledBounds.X, scaledBounds.Y, a.Width, a.Height, BoundsSpecified.All);
			this.MaximumSize = size2;
			this.MinimumSize = size;
		}

		/// <summary>Данный метод не относится к этому классу.</summary>
		/// <param name="dx">Коэффициент горизонтального масштабирования.</param>
		/// <param name="dy">Коэффициент вертикального масштабирования.</param>
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected virtual void ScaleCore(float dx, float dy)
		{
			this.SuspendLayout();
			try
			{
				int num = (int)Math.Round((double)((float)this.x * dx));
				int num2 = (int)Math.Round((double)((float)this.y * dy));
				int num3 = this.width;
				if ((this.controlStyle & ControlStyles.FixedWidth) != ControlStyles.FixedWidth)
				{
					num3 = (int)Math.Round((double)((float)(this.x + this.width) * dx)) - num;
				}
				int num4 = this.height;
				if ((this.controlStyle & ControlStyles.FixedHeight) != ControlStyles.FixedHeight)
				{
					num4 = (int)Math.Round((double)((float)(this.y + this.height) * dy)) - num2;
				}
				this.SetBounds(num, num2, num3, num4, BoundsSpecified.All);
				Control.ControlCollection controlCollection = (Control.ControlCollection)this.Properties.GetObject(Control.PropControlsCollection);
				if (controlCollection != null)
				{
					for (int i = 0; i < controlCollection.Count; i++)
					{
						controlCollection[i].Scale(dx, dy);
					}
				}
			}
			finally
			{
				this.ResumeLayout();
			}
		}

		internal Size ScaleSize(Size startSize, float x, float y)
		{
			Size result = startSize;
			if (!this.GetStyle(ControlStyles.FixedWidth))
			{
				result.Width = (int)Math.Round((double)((float)result.Width * x));
			}
			if (!this.GetStyle(ControlStyles.FixedHeight))
			{
				result.Height = (int)Math.Round((double)((float)result.Height * y));
			}
			return result;
		}

		/// <summary>Активирует элемент управления.</summary>
		/// <filterpriority>1</filterpriority>
		public void Select()
		{
			this.Select(false, false);
		}

		/// <summary>Активирует дочерний элемент управления.При необходимости указывает направление для выбора элементов управления в последовательности табуляции.</summary>
		/// <param name="directed">true, чтобы указать направление элемента управления, который требуется выделить; в противном случае — false. </param>
		/// <param name="forward">true для перемещения в прямом направлении последовательности перехода; false для перемещения в обратном направлении. </param>
		protected virtual void Select(bool directed, bool forward)
		{
			IContainerControl containerControlInternal = this.GetContainerControlInternal();
			if (containerControlInternal != null)
			{
				containerControlInternal.ActiveControl = this;
			}
		}

		/// <summary>Активирует следующий элемент управления.</summary>
		/// <returns>Значение true, если элемент управления был активирован; в противном случае — значение false.</returns>
		/// <param name="ctl">Объект <see cref="T:System.Windows.Forms.Control" />, с которого следует начать поиск. </param>
		/// <param name="forward">true для перемещения в прямом направлении последовательности перехода; false для перемещения в обратном направлении. </param>
		/// <param name="tabStopOnly">Значение true, чтобы игнорировать элементы управления, у которых для свойства <see cref="P:System.Windows.Forms.Control.TabStop" /> задано значение false; в противном случае — значение false. </param>
		/// <param name="nested">Значение true, чтобы включить вложенные дочерние элементы (т. е. дочерние элементы дочерних элементов); в противном случае — значение false. </param>
		/// <param name="wrap">Значение true для продолжения поиска с первого элемента управления в последовательности табуляции после достижения последнего элемента; в противном случае — значение false. </param>
		/// <filterpriority>1</filterpriority>
		public bool SelectNextControl(Control ctl, bool forward, bool tabStopOnly, bool nested, bool wrap)
		{
			if (!this.Contains(ctl) || (!nested && ctl.parent != this))
			{
				ctl = null;
			}
			bool flag = false;
			Control control = ctl;
			while (true)
			{
				ctl = this.GetNextControl(ctl, forward);
				if (ctl == null)
				{
					if (!wrap)
					{
						return false;
					}
					if (flag)
					{
						break;
					}
					flag = true;
				}
				else if (ctl.CanSelect && (!tabStopOnly || ctl.TabStop) && (nested || ctl.parent == this))
				{
					goto IL_57;
				}
				if (ctl == control)
				{
					return false;
				}
			}
			return false;
			IL_57:
			ctl.Select(true, forward);
			return true;
		}

		internal bool SelectNextControlInternal(Control ctl, bool forward, bool tabStopOnly, bool nested, bool wrap)
		{
			return this.SelectNextControl(ctl, forward, tabStopOnly, nested, wrap);
		}

		private void SelectNextIfFocused()
		{
			if (this.ContainsFocus && this.ParentInternal != null)
			{
				IContainerControl containerControlInternal = this.ParentInternal.GetContainerControlInternal();
				if (containerControlInternal != null)
				{
					((Control)containerControlInternal).SelectNextControlInternal(this, true, true, true, true);
				}
			}
		}

		internal IntPtr SendMessage(int msg, int wparam, int lparam)
		{
			return UnsafeNativeMethods.SendMessage(new HandleRef(this, this.Handle), msg, wparam, lparam);
		}

		internal IntPtr SendMessage(int msg, ref int wparam, ref int lparam)
		{
			return UnsafeNativeMethods.SendMessage(new HandleRef(this, this.Handle), msg, ref wparam, ref lparam);
		}

		internal IntPtr SendMessage(int msg, int wparam, IntPtr lparam)
		{
			return UnsafeNativeMethods.SendMessage(new HandleRef(this, this.Handle), msg, (IntPtr)wparam, lparam);
		}

		internal IntPtr SendMessage(int msg, IntPtr wparam, IntPtr lparam)
		{
			return UnsafeNativeMethods.SendMessage(new HandleRef(this, this.Handle), msg, wparam, lparam);
		}

		internal IntPtr SendMessage(int msg, IntPtr wparam, int lparam)
		{
			return UnsafeNativeMethods.SendMessage(new HandleRef(this, this.Handle), msg, wparam, (IntPtr)lparam);
		}

		internal IntPtr SendMessage(int msg, int wparam, ref NativeMethods.RECT lparam)
		{
			return UnsafeNativeMethods.SendMessage(new HandleRef(this, this.Handle), msg, wparam, ref lparam);
		}

		internal IntPtr SendMessage(int msg, bool wparam, int lparam)
		{
			return UnsafeNativeMethods.SendMessage(new HandleRef(this, this.Handle), msg, wparam, lparam);
		}

		internal IntPtr SendMessage(int msg, int wparam, string lparam)
		{
			return UnsafeNativeMethods.SendMessage(new HandleRef(this, this.Handle), msg, wparam, lparam);
		}

		/// <summary>Отправляет элемент управления в конец z-порядка.</summary>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void SendToBack()
		{
			if (this.parent != null)
			{
				this.parent.Controls.SetChildIndex(this, -1);
				return;
			}
			if (this.IsHandleCreated && this.GetTopLevel())
			{
				SafeNativeMethods.SetWindowPos(new HandleRef(this.window, this.Handle), NativeMethods.HWND_BOTTOM, 0, 0, 0, 0, 3);
			}
		}

		/// <summary>Задает границы элемента управления для указанного местоположения и размера.</summary>
		/// <param name="x">Новое значение свойства <see cref="P:System.Windows.Forms.Control.Left" /> элемента управления. </param>
		/// <param name="y">Новое значение свойства <see cref="P:System.Windows.Forms.Control.Top" /> элемента управления. </param>
		/// <param name="width">Новое значение свойства <see cref="P:System.Windows.Forms.Control.Width" /> элемента управления. </param>
		/// <param name="height">Новое значение свойства <see cref="P:System.Windows.Forms.Control.Height" /> элемента управления. </param>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void SetBounds(int x, int y, int width, int height)
		{
			if (this.x != x || this.y != y || this.width != width || this.height != height)
			{
				this.SetBoundsCore(x, y, width, height, BoundsSpecified.All);
				LayoutTransaction.DoLayout(this.ParentInternal, this, PropertyNames.Bounds);
				return;
			}
			this.InitScaling(BoundsSpecified.All);
		}

		/// <summary>Задает указанные границы элемента управления для указанного местоположения и размера.</summary>
		/// <param name="x">Новое значение свойства <see cref="P:System.Windows.Forms.Control.Left" /> элемента управления. </param>
		/// <param name="y">Новое значение свойства <see cref="P:System.Windows.Forms.Control.Top" /> элемента управления. </param>
		/// <param name="width">Новое значение свойства <see cref="P:System.Windows.Forms.Control.Width" /> элемента управления. </param>
		/// <param name="height">Новое значение свойства <see cref="P:System.Windows.Forms.Control.Height" /> элемента управления. </param>
		/// <param name="specified">Битовая комбинация значений <see cref="T:System.Windows.Forms.BoundsSpecified" />.Для любого неуказанного параметра будет использовано текущее значение.</param>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void SetBounds(int x, int y, int width, int height, BoundsSpecified specified)
		{
			if ((specified & BoundsSpecified.X) == BoundsSpecified.None)
			{
				x = this.x;
			}
			if ((specified & BoundsSpecified.Y) == BoundsSpecified.None)
			{
				y = this.y;
			}
			if ((specified & BoundsSpecified.Width) == BoundsSpecified.None)
			{
				width = this.width;
			}
			if ((specified & BoundsSpecified.Height) == BoundsSpecified.None)
			{
				height = this.height;
			}
			if (this.x != x || this.y != y || this.width != width || this.height != height)
			{
				this.SetBoundsCore(x, y, width, height, specified);
				LayoutTransaction.DoLayout(this.ParentInternal, this, PropertyNames.Bounds);
				return;
			}
			this.InitScaling(specified);
		}

		/// <summary>Задает указанные границы данного элемента управления.</summary>
		/// <param name="x">Новое значение свойства <see cref="P:System.Windows.Forms.Control.Left" /> элемента управления. </param>
		/// <param name="y">Новое значение свойства <see cref="P:System.Windows.Forms.Control.Top" /> элемента управления. </param>
		/// <param name="width">Новое значение свойства <see cref="P:System.Windows.Forms.Control.Width" /> элемента управления. </param>
		/// <param name="height">Новое значение свойства <see cref="P:System.Windows.Forms.Control.Height" /> элемента управления. </param>
		/// <param name="specified">Битовая комбинация значений <see cref="T:System.Windows.Forms.BoundsSpecified" />. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
		{
			if (this.ParentInternal != null)
			{
				this.ParentInternal.SuspendLayout();
			}
			try
			{
				if (this.x != x || this.y != y || this.width != width || this.height != height)
				{
					CommonProperties.UpdateSpecifiedBounds(this, x, y, width, height, specified);
					Rectangle rectangle = this.ApplyBoundsConstraints(x, y, width, height);
					width = rectangle.Width;
					height = rectangle.Height;
					x = rectangle.X;
					y = rectangle.Y;
					if (!this.IsHandleCreated)
					{
						this.UpdateBounds(x, y, width, height);
					}
					else if (!this.GetState(65536))
					{
						int num = 20;
						if (this.x == x && this.y == y)
						{
							num |= 2;
						}
						if (this.width == width && this.height == height)
						{
							num |= 1;
						}
						this.OnBoundsUpdate(x, y, width, height);
						SafeNativeMethods.SetWindowPos(new HandleRef(this.window, this.Handle), NativeMethods.NullHandleRef, x, y, width, height, num);
					}
				}
			}
			finally
			{
				this.InitScaling(specified);
				if (this.ParentInternal != null)
				{
					CommonProperties.xClearPreferredSizeCache(this.ParentInternal);
					this.ParentInternal.LayoutEngine.InitLayout(this, specified);
					this.ParentInternal.ResumeLayout(true);
				}
			}
		}

		/// <summary>Задает размер клиентской области элемента управления.</summary>
		/// <param name="x">Ширина клиентской области в точках. </param>
		/// <param name="y">Высота клиентской области в точках. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual void SetClientSizeCore(int x, int y)
		{
			this.Size = this.SizeFromClientSize(x, y);
			this.clientWidth = x;
			this.clientHeight = y;
			this.OnClientSizeChanged(EventArgs.Empty);
		}

		/// <summary>Определяет размер всего элемента управления по высоте и ширине его клиентской области.</summary>
		/// <returns>Значение <see cref="T:System.Drawing.Size" />, представляющее высоту и ширину всего элемента управления.</returns>
		/// <param name="clientSize">Значение <see cref="T:System.Drawing.Size" />, представляющее высоту и ширину клиентской области элемента управления.</param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected virtual Size SizeFromClientSize(Size clientSize)
		{
			return this.SizeFromClientSize(clientSize.Width, clientSize.Height);
		}

		internal Size SizeFromClientSize(int width, int height)
		{
			NativeMethods.RECT rECT = new NativeMethods.RECT(0, 0, width, height);
			CreateParams createParams = this.CreateParams;
			SafeNativeMethods.AdjustWindowRectEx(ref rECT, createParams.Style, this.HasMenu, createParams.ExStyle);
			return rECT.Size;
		}

		private void SetHandle(IntPtr value)
		{
			if (value == IntPtr.Zero)
			{
				this.SetState(1, false);
			}
			this.UpdateRoot();
		}

		private void SetParentHandle(IntPtr value)
		{
			if (this.IsHandleCreated)
			{
				IntPtr value2 = UnsafeNativeMethods.GetParent(new HandleRef(this.window, this.Handle));
				bool topLevel = this.GetTopLevel();
				if (value2 != value || (value2 == IntPtr.Zero && !topLevel))
				{
					bool flag = (value2 == IntPtr.Zero && !topLevel) || (value == IntPtr.Zero & topLevel);
					if (flag)
					{
						Form form = this as Form;
						if (form != null && !form.CanRecreateHandle())
						{
							flag = false;
							this.UpdateStyles();
						}
					}
					if (flag)
					{
						this.RecreateHandle();
					}
					if (!this.GetTopLevel())
					{
						if (value == IntPtr.Zero)
						{
							Application.ParkHandle(new HandleRef(this.window, this.Handle));
							this.UpdateRoot();
							return;
						}
						UnsafeNativeMethods.SetParent(new HandleRef(this.window, this.Handle), new HandleRef(null, value));
						if (this.parent != null)
						{
							this.parent.UpdateChildZOrder(this);
						}
						Application.UnparkHandle(new HandleRef(this.window, this.Handle));
						return;
					}
				}
				else if ((value == IntPtr.Zero && value2 == IntPtr.Zero) & topLevel)
				{
					UnsafeNativeMethods.SetParent(new HandleRef(this.window, this.Handle), new HandleRef(null, IntPtr.Zero));
					Application.UnparkHandle(new HandleRef(this.window, this.Handle));
				}
			}
		}

		internal void SetState(int flag, bool value)
		{
			this.state = (value ? (this.state | flag) : (this.state & ~flag));
		}

		internal void SetState2(int flag, bool value)
		{
			this.state2 = (value ? (this.state2 | flag) : (this.state2 & ~flag));
		}

		/// <summary>Задает указанный флаг <see cref="T:System.Windows.Forms.ControlStyles" /> либо значению true, либо значению false.</summary>
		/// <param name="flag">Задаваемый бит <see cref="T:System.Windows.Forms.ControlStyles" />. </param>
		/// <param name="value">Значение true, чтобы применить указанный стиль к элементу управления; в противном случае — значение false. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void SetStyle(ControlStyles flag, bool value)
		{
			if ((flag & ControlStyles.EnableNotifyMessage) > (ControlStyles)0 & value)
			{
				IntSecurity.UnmanagedCode.Demand();
			}
			this.controlStyle = (value ? (this.controlStyle | flag) : (this.controlStyle & ~flag));
		}

		internal static IntPtr SetUpPalette(IntPtr dc, bool force, bool realizePalette)
		{
			IntPtr halftonePalette = Graphics.GetHalftonePalette();
			IntPtr expr_20 = SafeNativeMethods.SelectPalette(new HandleRef(null, dc), new HandleRef(null, halftonePalette), force ? 0 : 1);
			if (expr_20 != IntPtr.Zero & realizePalette)
			{
				SafeNativeMethods.RealizePalette(new HandleRef(null, dc));
			}
			return expr_20;
		}

		/// <summary>Задает элемент управления как элемент верхнего уровня.</summary>
		/// <param name="value">Значение true, чтобы задать элемент управления как элемент верхнего уровня; в противном случае — значение false. </param>
		/// <exception cref="T:System.InvalidOperationException">Для параметра <paramref name="value" /> задано значение true, а элемент управления является элементом ActiveX. </exception>
		/// <exception cref="T:System.Exception">Возвращаемое значение <see cref="M:System.Windows.Forms.Control.GetTopLevel" /> не равно значению параметра <paramref name="value" />, а значение свойства <see cref="P:System.Windows.Forms.Control.Parent" /> не равно значению null. </exception>
		protected void SetTopLevel(bool value)
		{
			if (value && this.IsActiveX)
			{
				throw new InvalidOperationException(SR.GetString("TopLevelNotAllowedIfActiveX"));
			}
			if (value)
			{
				if (this is Form)
				{
					IntSecurity.TopLevelWindow.Demand();
				}
				else
				{
					IntSecurity.UnrestrictedWindows.Demand();
				}
			}
			this.SetTopLevelInternal(value);
		}

		internal void SetTopLevelInternal(bool value)
		{
			if (this.GetTopLevel() != value)
			{
				if (this.parent != null)
				{
					throw new ArgumentException(SR.GetString("TopLevelParentedControl"), "value");
				}
				this.SetState(524288, value);
				if (this.IsHandleCreated && this.GetState2(8))
				{
					this.ListenToUserPreferenceChanged(value);
				}
				this.UpdateStyles();
				this.SetParentHandle(IntPtr.Zero);
				if (value && this.Visible)
				{
					this.CreateControl();
				}
				this.UpdateRoot();
			}
		}

		/// <summary>Задает элемент управления в указанном видимом состоянии.</summary>
		/// <param name="value">Значение true, чтобы сделать элемент управления видимым; в противном случае — значение false. </param>
		protected virtual void SetVisibleCore(bool value)
		{
			try
			{
				System.Internal.HandleCollector.SuspendCollect();
				if (this.GetVisibleCore() != value)
				{
					if (!value)
					{
						this.SelectNextIfFocused();
					}
					bool flag = false;
					if (this.GetTopLevel())
					{
						if (this.IsHandleCreated | value)
						{
							SafeNativeMethods.ShowWindow(new HandleRef(this, this.Handle), value ? this.ShowParams : 0);
						}
					}
					else if (this.IsHandleCreated || (value && this.parent != null && this.parent.Created))
					{
						this.SetState(2, value);
						flag = true;
						try
						{
							if (value)
							{
								this.CreateControl();
							}
							SafeNativeMethods.SetWindowPos(new HandleRef(this.window, this.Handle), NativeMethods.NullHandleRef, 0, 0, 0, 0, 23 | (value ? 64 : 128));
						}
						catch
						{
							this.SetState(2, !value);
							throw;
						}
					}
					if (this.GetVisibleCore() != value)
					{
						this.SetState(2, value);
						flag = true;
					}
					if (flag)
					{
						using (new LayoutTransaction(this.parent, this, PropertyNames.Visible))
						{
							this.OnVisibleChanged(EventArgs.Empty);
						}
					}
					this.UpdateRoot();
				}
				else if (this.GetState(2) || value || !this.IsHandleCreated || SafeNativeMethods.IsWindowVisible(new HandleRef(this, this.Handle)))
				{
					this.SetState(2, value);
					if (this.IsHandleCreated)
					{
						SafeNativeMethods.SetWindowPos(new HandleRef(this.window, this.Handle), NativeMethods.NullHandleRef, 0, 0, 0, 0, 23 | (value ? 64 : 128));
					}
				}
			}
			finally
			{
				System.Internal.HandleCollector.ResumeCollect();
			}
		}

		internal static AutoValidate GetAutoValidateForControl(Control control)
		{
			ContainerControl parentContainerControl = control.ParentContainerControl;
			if (parentContainerControl == null)
			{
				return AutoValidate.EnablePreventFocusChange;
			}
			return parentContainerControl.AutoValidate;
		}

		internal virtual bool ShouldPerformContainerValidation()
		{
			return this.GetStyle(ControlStyles.ContainerControl);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal virtual bool ShouldSerializeBackColor()
		{
			return !this.Properties.GetColor(Control.PropBackColor).IsEmpty;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal virtual bool ShouldSerializeCursor()
		{
			bool flag;
			object @object = this.Properties.GetObject(Control.PropCursor, out flag);
			return flag && @object != null;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		private bool ShouldSerializeEnabled()
		{
			return !this.GetState(4);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal virtual bool ShouldSerializeForeColor()
		{
			return !this.Properties.GetColor(Control.PropForeColor).IsEmpty;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal virtual bool ShouldSerializeFont()
		{
			bool flag;
			object @object = this.Properties.GetObject(Control.PropFont, out flag);
			return flag && @object != null;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal virtual bool ShouldSerializeRightToLeft()
		{
			bool flag;
			int integer = this.Properties.GetInteger(Control.PropRightToLeft, out flag);
			return flag && integer != 2;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		private bool ShouldSerializeVisible()
		{
			return !this.GetState(2);
		}

		/// <summary>Преобразует указанный объект <see cref="T:System.Windows.Forms.HorizontalAlignment" /> в соответствующий объект <see cref="T:System.Windows.Forms.HorizontalAlignment" />, чтобы обеспечить поддержку текста, читаемого справа налево.</summary>
		/// <returns>Одно из значений <see cref="T:System.Windows.Forms.HorizontalAlignment" />.</returns>
		/// <param name="align">Одно из значений <see cref="T:System.Windows.Forms.HorizontalAlignment" />. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected HorizontalAlignment RtlTranslateAlignment(HorizontalAlignment align)
		{
			return this.RtlTranslateHorizontal(align);
		}

		/// <summary>Преобразует указанный объект <see cref="T:System.Windows.Forms.LeftRightAlignment" /> в соответствующий объект <see cref="T:System.Windows.Forms.LeftRightAlignment" />, чтобы обеспечить поддержку текста, читаемого справа налево.</summary>
		/// <returns>Одно из значений <see cref="T:System.Windows.Forms.LeftRightAlignment" />.</returns>
		/// <param name="align">Одно из значений <see cref="T:System.Windows.Forms.LeftRightAlignment" />. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected LeftRightAlignment RtlTranslateAlignment(LeftRightAlignment align)
		{
			return this.RtlTranslateLeftRight(align);
		}

		/// <summary>Преобразует указанный объект <see cref="T:System.Drawing.ContentAlignment" /> в соответствующий объект <see cref="T:System.Drawing.ContentAlignment" />, чтобы обеспечить поддержку текста, читаемого справа налево.</summary>
		/// <returns>Одно из значений <see cref="T:System.Drawing.ContentAlignment" />.</returns>
		/// <param name="align">Одно из значений <see cref="T:System.Drawing.ContentAlignment" />. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected ContentAlignment RtlTranslateAlignment(ContentAlignment align)
		{
			return this.RtlTranslateContent(align);
		}

		/// <summary>Преобразует указанный объект <see cref="T:System.Windows.Forms.HorizontalAlignment" /> в соответствующий объект <see cref="T:System.Windows.Forms.HorizontalAlignment" />, чтобы обеспечить поддержку текста, читаемого справа налево.</summary>
		/// <returns>Одно из значений <see cref="T:System.Windows.Forms.HorizontalAlignment" />.</returns>
		/// <param name="align">Одно из значений <see cref="T:System.Windows.Forms.HorizontalAlignment" />. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected HorizontalAlignment RtlTranslateHorizontal(HorizontalAlignment align)
		{
			if (RightToLeft.Yes == this.RightToLeft)
			{
				if (align == HorizontalAlignment.Left)
				{
					return HorizontalAlignment.Right;
				}
				if (HorizontalAlignment.Right == align)
				{
					return HorizontalAlignment.Left;
				}
			}
			return align;
		}

		/// <summary>Преобразует указанный объект <see cref="T:System.Windows.Forms.LeftRightAlignment" /> в соответствующий объект <see cref="T:System.Windows.Forms.LeftRightAlignment" />, чтобы обеспечить поддержку текста, читаемого справа налево.</summary>
		/// <returns>Одно из значений <see cref="T:System.Windows.Forms.LeftRightAlignment" />.</returns>
		/// <param name="align">Одно из значений <see cref="T:System.Windows.Forms.LeftRightAlignment" />. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected LeftRightAlignment RtlTranslateLeftRight(LeftRightAlignment align)
		{
			if (RightToLeft.Yes == this.RightToLeft)
			{
				if (align == LeftRightAlignment.Left)
				{
					return LeftRightAlignment.Right;
				}
				if (LeftRightAlignment.Right == align)
				{
					return LeftRightAlignment.Left;
				}
			}
			return align;
		}

		/// <summary>Преобразует указанный объект <see cref="T:System.Drawing.ContentAlignment" /> в соответствующий объект <see cref="T:System.Drawing.ContentAlignment" />, чтобы обеспечить поддержку текста, читаемого справа налево.</summary>
		/// <returns>Одно из значений <see cref="T:System.Drawing.ContentAlignment" />.</returns>
		/// <param name="align">Одно из значений <see cref="T:System.Drawing.ContentAlignment" />. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected internal ContentAlignment RtlTranslateContent(ContentAlignment align)
		{
			if (RightToLeft.Yes == this.RightToLeft)
			{
				if ((align & WindowsFormsUtils.AnyTopAlign) != (ContentAlignment)0)
				{
					if (align == ContentAlignment.TopLeft)
					{
						return ContentAlignment.TopRight;
					}
					if (align == ContentAlignment.TopRight)
					{
						return ContentAlignment.TopLeft;
					}
				}
				if ((align & WindowsFormsUtils.AnyMiddleAlign) != (ContentAlignment)0)
				{
					if (align == ContentAlignment.MiddleLeft)
					{
						return ContentAlignment.MiddleRight;
					}
					if (align == ContentAlignment.MiddleRight)
					{
						return ContentAlignment.MiddleLeft;
					}
				}
				if ((align & WindowsFormsUtils.AnyBottomAlign) != (ContentAlignment)0)
				{
					if (align == ContentAlignment.BottomLeft)
					{
						return ContentAlignment.BottomRight;
					}
					if (align == ContentAlignment.BottomRight)
					{
						return ContentAlignment.BottomLeft;
					}
				}
			}
			return align;
		}

		private void SetWindowFont()
		{
			this.SendMessage(48, this.FontHandle, 0);
		}

		private void SetWindowStyle(int flag, bool value)
		{
			int num = (int)((long)UnsafeNativeMethods.GetWindowLong(new HandleRef(this, this.Handle), -16));
			UnsafeNativeMethods.SetWindowLong(new HandleRef(this, this.Handle), -16, new HandleRef(null, (IntPtr)(value ? (num | flag) : (num & ~flag))));
		}

		/// <summary>Отображает элемент управления для пользователя.</summary>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void Show()
		{
			this.Visible = true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal bool ShouldSerializeMargin()
		{
			return !this.Margin.Equals(this.DefaultMargin);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal virtual bool ShouldSerializeMaximumSize()
		{
			return this.MaximumSize != this.DefaultMaximumSize;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal virtual bool ShouldSerializeMinimumSize()
		{
			return this.MinimumSize != this.DefaultMinimumSize;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal bool ShouldSerializePadding()
		{
			return !this.Padding.Equals(this.DefaultPadding);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal virtual bool ShouldSerializeSize()
		{
			Size defaultSize = this.DefaultSize;
			return this.width != defaultSize.Width || this.height != defaultSize.Height;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal virtual bool ShouldSerializeText()
		{
			return this.Text.Length != 0;
		}

		/// <summary>Временно приостанавливает логику макета для элемента управления.</summary>
		/// <filterpriority>1</filterpriority>
		public void SuspendLayout()
		{
			this.layoutSuspendCount += 1;
			if (this.layoutSuspendCount == 1)
			{
				this.OnLayoutSuspended();
			}
		}

		private void UnhookMouseEvent()
		{
			this.SetState(16384, false);
		}

		/// <summary>Вызывает перерисовку элементом управления недопустимых областей клиентской области.</summary>
		/// <filterpriority>1</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		public void Update()
		{
			SafeNativeMethods.UpdateWindow(new HandleRef(this.window, this.InternalHandle));
		}

		/// <summary>Обновляет границы элемента управления с учетом текущего размера и местоположения.</summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected internal void UpdateBounds()
		{
			NativeMethods.RECT rECT = default(NativeMethods.RECT);
			UnsafeNativeMethods.GetClientRect(new HandleRef(this.window, this.InternalHandle), ref rECT);
			int right = rECT.right;
			int bottom = rECT.bottom;
			UnsafeNativeMethods.GetWindowRect(new HandleRef(this.window, this.InternalHandle), ref rECT);
			if (!this.GetTopLevel())
			{
				UnsafeNativeMethods.MapWindowPoints(NativeMethods.NullHandleRef, new HandleRef(null, UnsafeNativeMethods.GetParent(new HandleRef(this.window, this.InternalHandle))), ref rECT, 2);
			}
			this.UpdateBounds(rECT.left, rECT.top, rECT.right - rECT.left, rECT.bottom - rECT.top, right, bottom);
		}

		/// <summary>Обновляет границы элемента управления с учетом указанного размера и местоположения.</summary>
		/// <param name="x">Координата <see cref="P:System.Drawing.Point.X" /> элемента управления. </param>
		/// <param name="y">Координата <see cref="P:System.Drawing.Point.Y" /> элемента управления. </param>
		/// <param name="width">Значение свойства <see cref="P:System.Drawing.Size.Width" /> элемента управления. </param>
		/// <param name="height">Значение свойства <see cref="P:System.Drawing.Size.Height" /> элемента управления. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void UpdateBounds(int x, int y, int width, int height)
		{
			NativeMethods.RECT rECT = default(NativeMethods.RECT);
			rECT.left = (rECT.right = (rECT.top = (rECT.bottom = 0)));
			CreateParams createParams = this.CreateParams;
			SafeNativeMethods.AdjustWindowRectEx(ref rECT, createParams.Style, false, createParams.ExStyle);
			int num = width - (rECT.right - rECT.left);
			int num2 = height - (rECT.bottom - rECT.top);
			this.UpdateBounds(x, y, width, height, num, num2);
		}

		/// <summary>Обновляет границы элемента управления с учетом указанного размера, местоположения и клиентского размера.</summary>
		/// <param name="x">Координата <see cref="P:System.Drawing.Point.X" /> элемента управления. </param>
		/// <param name="y">Координата <see cref="P:System.Drawing.Point.Y" /> элемента управления. </param>
		/// <param name="width">Значение свойства <see cref="P:System.Drawing.Size.Width" /> элемента управления. </param>
		/// <param name="height">Значение свойства <see cref="P:System.Drawing.Size.Height" /> элемента управления. </param>
		/// <param name="clientWidth">Клиентское свойство <see cref="P:System.Drawing.Size.Width" /> элемента управления. </param>
		/// <param name="clientHeight">Клиентское свойство <see cref="P:System.Drawing.Size.Height" /> элемента управления. </param>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void UpdateBounds(int x, int y, int width, int height, int clientWidth, int clientHeight)
		{
			bool flag = this.x != x || this.y != y;
			bool arg_81_0 = this.Width != width || this.Height != height || this.clientWidth != clientWidth || this.clientHeight != clientHeight;
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
			this.clientWidth = clientWidth;
			this.clientHeight = clientHeight;
			if (flag)
			{
				this.OnLocationChanged(EventArgs.Empty);
			}
			if (arg_81_0)
			{
				this.OnSizeChanged(EventArgs.Empty);
				this.OnClientSizeChanged(EventArgs.Empty);
				CommonProperties.xClearPreferredSizeCache(this);
				LayoutTransaction.DoLayout(this.ParentInternal, this, PropertyNames.Bounds);
			}
		}

		private void UpdateBindings()
		{
			for (int i = 0; i < this.DataBindings.Count; i++)
			{
				BindingContext.UpdateBinding(this.BindingContext, this.DataBindings[i]);
			}
		}

		private void UpdateChildControlIndex(Control ctl)
		{
			int num = 0;
			int childIndex = this.Controls.GetChildIndex(ctl);
			IntPtr internalHandle = ctl.InternalHandle;
			while ((internalHandle = UnsafeNativeMethods.GetWindow(new HandleRef(null, internalHandle), 3)) != IntPtr.Zero)
			{
				Control control = Control.FromHandleInternal(internalHandle);
				if (control != null)
				{
					num = this.Controls.GetChildIndex(control, false) + 1;
					break;
				}
			}
			if (num > childIndex)
			{
				num--;
			}
			if (num != childIndex)
			{
				this.Controls.SetChildIndex(ctl, num);
			}
		}

		private void UpdateReflectParent(bool findNewParent)
		{
			if ((!this.Disposing & findNewParent) && this.IsHandleCreated)
			{
				IntPtr intPtr = UnsafeNativeMethods.GetParent(new HandleRef(this, this.Handle));
				if (intPtr != IntPtr.Zero)
				{
					this.ReflectParent = Control.FromHandleInternal(intPtr);
					return;
				}
			}
			this.ReflectParent = null;
		}

		/// <summary>Обновляет элемент управления в z-порядке его родительского элемента управления.</summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void UpdateZOrder()
		{
			if (this.parent != null)
			{
				this.parent.UpdateChildZOrder(this);
			}
		}

		private void UpdateChildZOrder(Control ctl)
		{
			if (!this.IsHandleCreated || !ctl.IsHandleCreated || ctl.parent != this)
			{
				return;
			}
			IntPtr intPtr = (IntPtr)NativeMethods.HWND_TOP;
			int num = this.Controls.GetChildIndex(ctl);
			while (--num >= 0)
			{
				Control control = this.Controls[num];
				if (control.IsHandleCreated && control.parent == this)
				{
					intPtr = control.Handle;
					break;
				}
			}
			if (UnsafeNativeMethods.GetWindow(new HandleRef(ctl.window, ctl.Handle), 3) != intPtr)
			{
				this.state |= 256;
				try
				{
					SafeNativeMethods.SetWindowPos(new HandleRef(ctl.window, ctl.Handle), new HandleRef(null, intPtr), 0, 0, 0, 0, 3);
				}
				finally
				{
					this.state &= -257;
				}
			}
		}

		private void UpdateRoot()
		{
			this.window.LockReference(this.GetTopLevel() && this.Visible);
		}

		/// <summary>Вызывает принудительное повторное применение назначенных стилей к элементу управления.</summary>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		protected void UpdateStyles()
		{
			this.UpdateStylesCore();
			this.OnStyleChanged(EventArgs.Empty);
		}

		internal virtual void UpdateStylesCore()
		{
			if (this.IsHandleCreated)
			{
				CreateParams createParams = this.CreateParams;
				int arg_41_0 = this.WindowStyle;
				int windowExStyle = this.WindowExStyle;
				if ((this.state & 2) != 0)
				{
					createParams.Style |= 268435456;
				}
				if (arg_41_0 != createParams.Style)
				{
					this.WindowStyle = createParams.Style;
				}
				if (windowExStyle != createParams.ExStyle)
				{
					this.WindowExStyle = createParams.ExStyle;
					this.SetState(1073741824, (createParams.ExStyle & 4194304) != 0);
				}
				SafeNativeMethods.SetWindowPos(new HandleRef(this, this.Handle), NativeMethods.NullHandleRef, 0, 0, 0, 0, 55);
				this.Invalidate(true);
			}
		}

		private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs pref)
		{
			if (pref.Category == UserPreferenceCategory.Color)
			{
				Control.defaultFont = null;
				this.OnSystemColorsChanged(EventArgs.Empty);
			}
		}

		internal virtual void OnBoundsUpdate(int x, int y, int width, int height)
		{
		}

		internal void WindowAssignHandle(IntPtr handle, bool value)
		{
			this.window.AssignHandle(handle, value);
		}

		internal void WindowReleaseHandle()
		{
			this.window.ReleaseHandle();
		}

		private void WmClose(ref Message m)
		{
			if (this.ParentInternal != null)
			{
				IntPtr handle = this.Handle;
				IntPtr intPtr = handle;
				while (handle != IntPtr.Zero)
				{
					intPtr = handle;
					handle = UnsafeNativeMethods.GetParent(new HandleRef(null, handle));
					if (((int)((long)UnsafeNativeMethods.GetWindowLong(new HandleRef(null, intPtr), -16)) & 1073741824) == 0)
					{
						break;
					}
				}
				if (intPtr != IntPtr.Zero)
				{
					UnsafeNativeMethods.PostMessage(new HandleRef(null, intPtr), 16, IntPtr.Zero, IntPtr.Zero);
				}
			}
			this.DefWndProc(ref m);
		}

		private void WmCaptureChanged(ref Message m)
		{
			this.OnMouseCaptureChanged(EventArgs.Empty);
			this.DefWndProc(ref m);
		}

		private void WmCommand(ref Message m)
		{
			if (IntPtr.Zero == m.LParam)
			{
				if (Command.DispatchID(NativeMethods.Util.LOWORD(m.WParam)))
				{
					return;
				}
			}
			else if (Control.ReflectMessageInternal(m.LParam, ref m))
			{
				return;
			}
			this.DefWndProc(ref m);
		}

		internal virtual void WmContextMenu(ref Message m)
		{
			this.WmContextMenu(ref m, this);
		}

		internal void WmContextMenu(ref Message m, Control sourceControl)
		{
			ContextMenu contextMenu = this.Properties.GetObject(Control.PropContextMenu) as ContextMenu;
			ContextMenuStrip contextMenuStrip = (contextMenu != null) ? null : (this.Properties.GetObject(Control.PropContextMenuStrip) as ContextMenuStrip);
			if (contextMenu == null && contextMenuStrip == null)
			{
				this.DefWndProc(ref m);
				return;
			}
			int num = NativeMethods.Util.SignedLOWORD(m.LParam);
			int num2 = NativeMethods.Util.SignedHIWORD(m.LParam);
			bool isKeyboardActivated = false;
			Point point;
			if ((int)((long)m.LParam) == -1)
			{
				isKeyboardActivated = true;
				point = new Point(this.Width / 2, this.Height / 2);
			}
			else
			{
				point = this.PointToClientInternal(new Point(num, num2));
			}
			if (!this.ClientRectangle.Contains(point))
			{
				this.DefWndProc(ref m);
				return;
			}
			if (contextMenu != null)
			{
				contextMenu.Show(sourceControl, point);
				return;
			}
			if (contextMenuStrip != null)
			{
				contextMenuStrip.ShowInternal(sourceControl, point, isKeyboardActivated);
				return;
			}
			this.DefWndProc(ref m);
		}

		private void WmCtlColorControl(ref Message m)
		{
			Control control = Control.FromHandleInternal(m.LParam);
			if (control != null)
			{
				m.Result = control.InitializeDCForWmCtlColor(m.WParam, m.Msg);
				if (m.Result != IntPtr.Zero)
				{
					return;
				}
			}
			this.DefWndProc(ref m);
		}

		private void WmDisplayChange(ref Message m)
		{
			BufferedGraphicsManager.Current.Invalidate();
			this.DefWndProc(ref m);
		}

		private void WmDrawItem(ref Message m)
		{
			if (m.WParam == IntPtr.Zero)
			{
				this.WmDrawItemMenuItem(ref m);
				return;
			}
			this.WmOwnerDraw(ref m);
		}

		private void WmDrawItemMenuItem(ref Message m)
		{
			MenuItem menuItemFromItemData = MenuItem.GetMenuItemFromItemData(((NativeMethods.DRAWITEMSTRUCT)m.GetLParam(typeof(NativeMethods.DRAWITEMSTRUCT))).itemData);
			if (menuItemFromItemData != null)
			{
				menuItemFromItemData.WmDrawItem(ref m);
			}
		}

		private void WmEraseBkgnd(ref Message m)
		{
			if (this.GetStyle(ControlStyles.UserPaint))
			{
				if (!this.GetStyle(ControlStyles.AllPaintingInWmPaint))
				{
					IntPtr wParam = m.WParam;
					if (wParam == IntPtr.Zero)
					{
						m.Result = (IntPtr)0;
						return;
					}
					NativeMethods.RECT rECT = default(NativeMethods.RECT);
					UnsafeNativeMethods.GetClientRect(new HandleRef(this, this.Handle), ref rECT);
					using (PaintEventArgs paintEventArgs = new PaintEventArgs(wParam, Rectangle.FromLTRB(rECT.left, rECT.top, rECT.right, rECT.bottom)))
					{
						this.PaintWithErrorHandling(paintEventArgs, 1);
					}
				}
				m.Result = (IntPtr)1;
				return;
			}
			this.DefWndProc(ref m);
		}

		private void WmExitMenuLoop(ref Message m)
		{
			if ((int)((long)m.WParam) != 0)
			{
				ContextMenu contextMenu = (ContextMenu)this.Properties.GetObject(Control.PropContextMenu);
				if (contextMenu != null)
				{
					contextMenu.OnCollapse(EventArgs.Empty);
				}
			}
			this.DefWndProc(ref m);
		}

		private void WmGetControlName(ref Message m)
		{
			string text;
			if (this.Site != null)
			{
				text = this.Site.Name;
			}
			else
			{
				text = this.Name;
			}
			if (text == null)
			{
				text = "";
			}
			this.MarshalStringToMessage(text, ref m);
		}

		private void WmGetControlType(ref Message m)
		{
			string assemblyQualifiedName = base.GetType().AssemblyQualifiedName;
			this.MarshalStringToMessage(assemblyQualifiedName, ref m);
		}

		private void WmGetObject(ref Message m)
		{
			InternalAccessibleObject internalAccessibleObject = null;
			AccessibleObject accessibilityObject = this.GetAccessibilityObject((int)((long)m.LParam));
			if (accessibilityObject != null)
			{
				IntSecurity.UnmanagedCode.Assert();
				try
				{
					internalAccessibleObject = new InternalAccessibleObject(accessibilityObject);
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
				}
			}
			if (internalAccessibleObject != null)
			{
				Guid guid = new Guid("{618736E0-3C3D-11CF-810C-00AA00389B71}");
				try
				{
					if (internalAccessibleObject is IAccessible)
					{
						throw new InvalidOperationException(SR.GetString("ControlAccessibileObjectInvalid"));
					}
					UnsafeNativeMethods.IAccessibleInternal accessibleInternal = internalAccessibleObject;
					if (accessibleInternal == null)
					{
						m.Result = (IntPtr)0;
					}
					else
					{
						IntPtr iUnknownForObject = Marshal.GetIUnknownForObject(accessibleInternal);
						IntSecurity.UnmanagedCode.Assert();
						try
						{
							m.Result = UnsafeNativeMethods.LresultFromObject(ref guid, m.WParam, new HandleRef(accessibilityObject, iUnknownForObject));
						}
						finally
						{
							CodeAccessPermission.RevertAssert();
							Marshal.Release(iUnknownForObject);
						}
					}
					return;
				}
				catch (Exception innerException)
				{
					throw new InvalidOperationException(SR.GetString("RichControlLresult"), innerException);
				}
			}
			this.DefWndProc(ref m);
		}

		private void WmHelp(ref Message m)
		{
			HelpInfo helpInfo = MessageBox.HelpInfo;
			if (helpInfo != null)
			{
				switch (helpInfo.Option)
				{
				case 1:
					Help.ShowHelp(this, helpInfo.HelpFilePath);
					break;
				case 2:
					Help.ShowHelp(this, helpInfo.HelpFilePath, helpInfo.Keyword);
					break;
				case 3:
					Help.ShowHelp(this, helpInfo.HelpFilePath, helpInfo.Navigator);
					break;
				case 4:
					Help.ShowHelp(this, helpInfo.HelpFilePath, helpInfo.Navigator, helpInfo.Param);
					break;
				}
			}
			NativeMethods.HELPINFO hELPINFO = (NativeMethods.HELPINFO)m.GetLParam(typeof(NativeMethods.HELPINFO));
			HelpEventArgs helpEventArgs = new HelpEventArgs(new Point(hELPINFO.MousePos.x, hELPINFO.MousePos.y));
			this.OnHelpRequested(helpEventArgs);
			if (!helpEventArgs.Handled)
			{
				this.DefWndProc(ref m);
			}
		}

		private void WmInitMenuPopup(ref Message m)
		{
			ContextMenu contextMenu = (ContextMenu)this.Properties.GetObject(Control.PropContextMenu);
			if (contextMenu != null && contextMenu.ProcessInitMenuPopup(m.WParam))
			{
				return;
			}
			this.DefWndProc(ref m);
		}

		private void WmMeasureItem(ref Message m)
		{
			if (m.WParam == IntPtr.Zero)
			{
				MenuItem menuItemFromItemData = MenuItem.GetMenuItemFromItemData(((NativeMethods.MEASUREITEMSTRUCT)m.GetLParam(typeof(NativeMethods.MEASUREITEMSTRUCT))).itemData);
				if (menuItemFromItemData != null)
				{
					menuItemFromItemData.WmMeasureItem(ref m);
					return;
				}
			}
			else
			{
				this.WmOwnerDraw(ref m);
			}
		}

		private void WmMenuChar(ref Message m)
		{
			Menu contextMenu = this.ContextMenu;
			if (contextMenu != null)
			{
				contextMenu.WmMenuChar(ref m);
				m.Result != IntPtr.Zero;
				return;
			}
		}

		private void WmMenuSelect(ref Message m)
		{
			int num = NativeMethods.Util.LOWORD(m.WParam);
			int num2 = NativeMethods.Util.HIWORD(m.WParam);
			IntPtr lParam = m.LParam;
			MenuItem menuItem = null;
			if ((num2 & 8192) == 0)
			{
				if ((num2 & 16) == 0)
				{
					Command commandFromID = Command.GetCommandFromID(num);
					if (commandFromID != null)
					{
						object target = commandFromID.Target;
						if (target != null && target is MenuItem.MenuItemData)
						{
							menuItem = ((MenuItem.MenuItemData)target).baseItem;
						}
					}
				}
				else
				{
					menuItem = this.GetMenuItemFromHandleId(lParam, num);
				}
			}
			if (menuItem != null)
			{
				menuItem.PerformSelect();
			}
			this.DefWndProc(ref m);
		}

		private void WmCreate(ref Message m)
		{
			this.DefWndProc(ref m);
			if (this.parent != null)
			{
				this.parent.UpdateChildZOrder(this);
			}
			this.UpdateBounds();
			this.OnHandleCreated(EventArgs.Empty);
			if (!this.GetStyle(ControlStyles.CacheText))
			{
				this.text = null;
			}
		}

		private void WmDestroy(ref Message m)
		{
			if (!this.RecreatingHandle && !this.Disposing && !this.IsDisposed && this.GetState(16384))
			{
				this.OnMouseLeave(EventArgs.Empty);
				this.UnhookMouseEvent();
			}
			this.OnHandleDestroyed(EventArgs.Empty);
			if (!this.Disposing)
			{
				if (!this.RecreatingHandle)
				{
					this.SetState(1, false);
				}
			}
			else
			{
				this.SetState(2, false);
			}
			this.DefWndProc(ref m);
		}

		private void WmKeyChar(ref Message m)
		{
			if (this.ProcessKeyMessage(ref m))
			{
				return;
			}
			this.DefWndProc(ref m);
		}

		private void WmKillFocus(ref Message m)
		{
			this.WmImeKillFocus();
			this.DefWndProc(ref m);
			this.OnLostFocus(EventArgs.Empty);
		}

		private void WmMouseDown(ref Message m, MouseButtons button, int clicks)
		{
			MouseButtons mouseButtons = Control.MouseButtons;
			this.SetState(134217728, true);
			if (!this.GetStyle(ControlStyles.UserMouse))
			{
				this.DefWndProc(ref m);
				if (this.IsDisposed)
				{
					return;
				}
			}
			else if (button == MouseButtons.Left && this.GetStyle(ControlStyles.Selectable))
			{
				this.FocusInternal();
			}
			if (mouseButtons != Control.MouseButtons)
			{
				return;
			}
			if (!this.GetState2(16))
			{
				this.CaptureInternal = true;
			}
			if (mouseButtons != Control.MouseButtons)
			{
				return;
			}
			if (this.Enabled)
			{
				this.OnMouseDown(new MouseEventArgs(button, clicks, NativeMethods.Util.SignedLOWORD(m.LParam), NativeMethods.Util.SignedHIWORD(m.LParam), 0));
			}
		}

		private void WmMouseEnter(ref Message m)
		{
			this.DefWndProc(ref m);
			this.OnMouseEnter(EventArgs.Empty);
		}

		private void WmMouseLeave(ref Message m)
		{
			this.DefWndProc(ref m);
			this.OnMouseLeave(EventArgs.Empty);
		}

		private void WmMouseHover(ref Message m)
		{
			this.DefWndProc(ref m);
			this.OnMouseHover(EventArgs.Empty);
		}

		private void WmMouseMove(ref Message m)
		{
			if (!this.GetStyle(ControlStyles.UserMouse))
			{
				this.DefWndProc(ref m);
			}
			this.OnMouseMove(new MouseEventArgs(Control.MouseButtons, 0, NativeMethods.Util.SignedLOWORD(m.LParam), NativeMethods.Util.SignedHIWORD(m.LParam), 0));
		}

		private void WmMouseUp(ref Message m, MouseButtons button, int clicks)
		{
			try
			{
				int num = NativeMethods.Util.SignedLOWORD(m.LParam);
				int num2 = NativeMethods.Util.SignedHIWORD(m.LParam);
				Point p = new Point(num, num2);
				p = this.PointToScreen(p);
				if (!this.GetStyle(ControlStyles.UserMouse))
				{
					this.DefWndProc(ref m);
				}
				else if (button == MouseButtons.Right)
				{
					this.SendMessage(123, this.Handle, NativeMethods.Util.MAKELPARAM(p.X, p.Y));
				}
				bool flag = false;
				if ((this.controlStyle & ControlStyles.StandardClick) == ControlStyles.StandardClick && this.GetState(134217728) && !this.IsDisposed && UnsafeNativeMethods.WindowFromPoint(p.X, p.Y) == this.Handle)
				{
					flag = true;
				}
				if (flag && !this.ValidationCancelled)
				{
					if (!this.GetState(67108864))
					{
						this.OnClick(new MouseEventArgs(button, clicks, NativeMethods.Util.SignedLOWORD(m.LParam), NativeMethods.Util.SignedHIWORD(m.LParam), 0));
						this.OnMouseClick(new MouseEventArgs(button, clicks, NativeMethods.Util.SignedLOWORD(m.LParam), NativeMethods.Util.SignedHIWORD(m.LParam), 0));
					}
					else
					{
						this.OnDoubleClick(new MouseEventArgs(button, 2, NativeMethods.Util.SignedLOWORD(m.LParam), NativeMethods.Util.SignedHIWORD(m.LParam), 0));
						this.OnMouseDoubleClick(new MouseEventArgs(button, 2, NativeMethods.Util.SignedLOWORD(m.LParam), NativeMethods.Util.SignedHIWORD(m.LParam), 0));
					}
				}
				this.OnMouseUp(new MouseEventArgs(button, clicks, NativeMethods.Util.SignedLOWORD(m.LParam), NativeMethods.Util.SignedHIWORD(m.LParam), 0));
			}
			finally
			{
				this.SetState(67108864, false);
				this.SetState(134217728, false);
				this.SetState(268435456, false);
				this.CaptureInternal = false;
			}
		}

		private void WmMouseWheel(ref Message m)
		{
			Point p = new Point(NativeMethods.Util.SignedLOWORD(m.LParam), NativeMethods.Util.SignedHIWORD(m.LParam));
			p = this.PointToClient(p);
			HandledMouseEventArgs handledMouseEventArgs = new HandledMouseEventArgs(MouseButtons.None, 0, p.X, p.Y, NativeMethods.Util.SignedHIWORD(m.WParam));
			this.OnMouseWheel(handledMouseEventArgs);
			m.Result = (IntPtr)(handledMouseEventArgs.Handled ? 0 : 1);
			if (!handledMouseEventArgs.Handled)
			{
				this.DefWndProc(ref m);
			}
		}

		private void WmMove(ref Message m)
		{
			this.DefWndProc(ref m);
			this.UpdateBounds();
		}

		private unsafe void WmNotify(ref Message m)
		{
			NativeMethods.NMHDR* ptr = (NativeMethods.NMHDR*)((void*)m.LParam);
			if (!Control.ReflectMessageInternal(ptr->hwndFrom, ref m))
			{
				if (ptr->code == -521)
				{
					m.Result = UnsafeNativeMethods.SendMessage(new HandleRef(null, ptr->hwndFrom), 8192 + m.Msg, m.WParam, m.LParam);
					return;
				}
				if (ptr->code == -522)
				{
					UnsafeNativeMethods.SendMessage(new HandleRef(null, ptr->hwndFrom), 8192 + m.Msg, m.WParam, m.LParam);
				}
				this.DefWndProc(ref m);
			}
		}

		private void WmNotifyFormat(ref Message m)
		{
			if (!Control.ReflectMessageInternal(m.WParam, ref m))
			{
				this.DefWndProc(ref m);
			}
		}

		private void WmOwnerDraw(ref Message m)
		{
			bool flag = false;
			int num = (int)((long)m.WParam);
			IntPtr intPtr = UnsafeNativeMethods.GetDlgItem(new HandleRef(null, m.HWnd), num);
			if (intPtr == IntPtr.Zero)
			{
				intPtr = (IntPtr)((long)num);
			}
			if (!Control.ReflectMessageInternal(intPtr, ref m))
			{
				IntPtr handleFromID = this.window.GetHandleFromID((short)NativeMethods.Util.LOWORD(m.WParam));
				if (handleFromID != IntPtr.Zero)
				{
					Control control = Control.FromHandleInternal(handleFromID);
					if (control != null)
					{
						m.Result = control.SendMessage(8192 + m.Msg, handleFromID, m.LParam);
						flag = true;
					}
				}
			}
			else
			{
				flag = true;
			}
			if (!flag)
			{
				this.DefWndProc(ref m);
			}
		}

		private void WmPaint(ref Message m)
		{
			bool flag = this.DoubleBuffered || (this.GetStyle(ControlStyles.AllPaintingInWmPaint) && this.DoubleBufferingEnabled);
			IntPtr handle = IntPtr.Zero;
			NativeMethods.PAINTSTRUCT pAINTSTRUCT = default(NativeMethods.PAINTSTRUCT);
			bool flag2 = false;
			try
			{
				IntPtr intPtr;
				Rectangle rectangle;
				if (m.WParam == IntPtr.Zero)
				{
					handle = this.Handle;
					intPtr = UnsafeNativeMethods.BeginPaint(new HandleRef(this, handle), ref pAINTSTRUCT);
					flag2 = true;
					rectangle = new Rectangle(pAINTSTRUCT.rcPaint_left, pAINTSTRUCT.rcPaint_top, pAINTSTRUCT.rcPaint_right - pAINTSTRUCT.rcPaint_left, pAINTSTRUCT.rcPaint_bottom - pAINTSTRUCT.rcPaint_top);
				}
				else
				{
					intPtr = m.WParam;
					rectangle = this.ClientRectangle;
				}
				if (!flag || (rectangle.Width > 0 && rectangle.Height > 0))
				{
					IntPtr intPtr2 = IntPtr.Zero;
					BufferedGraphics bufferedGraphics = null;
					PaintEventArgs paintEventArgs = null;
					GraphicsState graphicsState = null;
					try
					{
						if (flag || m.WParam == IntPtr.Zero)
						{
							intPtr2 = Control.SetUpPalette(intPtr, false, false);
						}
						if (flag)
						{
							try
							{
								bufferedGraphics = this.BufferContext.Allocate(intPtr, this.ClientRectangle);
							}
							catch (Exception ex)
							{
								if (ClientUtils.IsCriticalException(ex) && !(ex is OutOfMemoryException))
								{
									throw;
								}
								flag = false;
							}
						}
						if (bufferedGraphics != null)
						{
							bufferedGraphics.Graphics.SetClip(rectangle);
							paintEventArgs = new PaintEventArgs(bufferedGraphics.Graphics, rectangle);
							graphicsState = paintEventArgs.Graphics.Save();
						}
						else
						{
							paintEventArgs = new PaintEventArgs(intPtr, rectangle);
						}
						using (paintEventArgs)
						{
							try
							{
								if ((m.WParam == IntPtr.Zero && this.GetStyle(ControlStyles.AllPaintingInWmPaint)) | flag)
								{
									this.PaintWithErrorHandling(paintEventArgs, 1);
								}
							}
							finally
							{
								if (graphicsState != null)
								{
									paintEventArgs.Graphics.Restore(graphicsState);
								}
								else
								{
									paintEventArgs.ResetGraphics();
								}
							}
							this.PaintWithErrorHandling(paintEventArgs, 2);
							if (bufferedGraphics != null)
							{
								bufferedGraphics.Render();
							}
						}
					}
					finally
					{
						if (intPtr2 != IntPtr.Zero)
						{
							SafeNativeMethods.SelectPalette(new HandleRef(null, intPtr), new HandleRef(null, intPtr2), 0);
						}
						if (bufferedGraphics != null)
						{
							bufferedGraphics.Dispose();
						}
					}
				}
			}
			finally
			{
				if (flag2)
				{
					UnsafeNativeMethods.EndPaint(new HandleRef(this, handle), ref pAINTSTRUCT);
				}
			}
		}

		private void WmPrintClient(ref Message m)
		{
			using (PaintEventArgs paintEventArgs = new Control.PrintPaintEventArgs(m, m.WParam, this.ClientRectangle))
			{
				this.OnPrint(paintEventArgs);
			}
		}

		private void WmQueryNewPalette(ref Message m)
		{
			IntPtr dC = UnsafeNativeMethods.GetDC(new HandleRef(this, this.Handle));
			try
			{
				Control.SetUpPalette(dC, true, true);
			}
			finally
			{
				UnsafeNativeMethods.ReleaseDC(new HandleRef(this, this.Handle), new HandleRef(null, dC));
			}
			this.Invalidate(true);
			m.Result = (IntPtr)1;
			this.DefWndProc(ref m);
		}

		private void WmSetCursor(ref Message m)
		{
			if (m.WParam == this.InternalHandle && NativeMethods.Util.LOWORD(m.LParam) == 1)
			{
				Cursor.CurrentInternal = this.Cursor;
				return;
			}
			this.DefWndProc(ref m);
		}

		private unsafe void WmWindowPosChanging(ref Message m)
		{
			if (this.IsActiveX)
			{
				NativeMethods.WINDOWPOS* ptr = (NativeMethods.WINDOWPOS*)((void*)m.LParam);
				bool flag = false;
				if ((ptr->flags & 2) == 0 && (ptr->x != this.Left || ptr->y != this.Top))
				{
					flag = true;
				}
				if ((ptr->flags & 1) == 0 && (ptr->cx != this.Width || ptr->cy != this.Height))
				{
					flag = true;
				}
				if (flag)
				{
					this.ActiveXUpdateBounds(ref ptr->x, ref ptr->y, ref ptr->cx, ref ptr->cy, ptr->flags);
				}
			}
			this.DefWndProc(ref m);
		}

		private void WmParentNotify(ref Message m)
		{
			int num = NativeMethods.Util.LOWORD(m.WParam);
			IntPtr intPtr = IntPtr.Zero;
			if (num != 1)
			{
				if (num != 2)
				{
					intPtr = UnsafeNativeMethods.GetDlgItem(new HandleRef(this, this.Handle), NativeMethods.Util.HIWORD(m.WParam));
				}
			}
			else
			{
				intPtr = m.LParam;
			}
			if (intPtr == IntPtr.Zero || !Control.ReflectMessageInternal(intPtr, ref m))
			{
				this.DefWndProc(ref m);
			}
		}

		private void WmSetFocus(ref Message m)
		{
			this.WmImeSetFocus();
			if (!this.HostedInWin32DialogManager)
			{
				IContainerControl containerControlInternal = this.GetContainerControlInternal();
				if (containerControlInternal != null)
				{
					ContainerControl containerControl = containerControlInternal as ContainerControl;
					bool flag;
					if (containerControl != null)
					{
						flag = containerControl.ActivateControlInternal(this);
					}
					else
					{
						IntSecurity.ModifyFocus.Assert();
						try
						{
							flag = containerControlInternal.ActivateControl(this);
						}
						finally
						{
							CodeAccessPermission.RevertAssert();
						}
					}
					if (!flag)
					{
						return;
					}
				}
			}
			this.DefWndProc(ref m);
			this.OnGotFocus(EventArgs.Empty);
		}

		private void WmShowWindow(ref Message m)
		{
			this.DefWndProc(ref m);
			if ((this.state & 16) == 0)
			{
				bool flag = m.WParam != IntPtr.Zero;
				bool visible = this.Visible;
				if (flag)
				{
					bool value = this.GetState(2);
					this.SetState(2, true);
					bool flag2 = false;
					try
					{
						this.CreateControl();
						flag2 = true;
						goto IL_81;
					}
					finally
					{
						if (!flag2)
						{
							this.SetState(2, value);
						}
					}
				}
				bool flag3 = this.GetTopLevel();
				if (this.ParentInternal != null)
				{
					flag3 = this.ParentInternal.Visible;
				}
				if (flag3)
				{
					this.SetState(2, false);
				}
				IL_81:
				if (!this.GetState(536870912) && visible != flag)
				{
					this.OnVisibleChanged(EventArgs.Empty);
				}
			}
		}

		private void WmUpdateUIState(ref Message m)
		{
			bool flag = false;
			bool flag2 = false;
			bool flag3 = (this.uiCuesState & 240) != 0;
			bool flag4 = (this.uiCuesState & 15) != 0;
			if (flag3)
			{
				flag = this.ShowKeyboardCues;
			}
			if (flag4)
			{
				flag2 = this.ShowFocusCues;
			}
			this.DefWndProc(ref m);
			int num = NativeMethods.Util.LOWORD(m.WParam);
			if (num == 3)
			{
				return;
			}
			UICues uICues = UICues.None;
			if ((NativeMethods.Util.HIWORD(m.WParam) & 2) != 0)
			{
				bool flag5 = num == 2;
				if (flag5 != flag || !flag3)
				{
					uICues |= UICues.ChangeKeyboard;
					this.uiCuesState &= -241;
					this.uiCuesState |= (flag5 ? 32 : 16);
				}
				if (flag5)
				{
					uICues |= UICues.ShowKeyboard;
				}
			}
			if ((NativeMethods.Util.HIWORD(m.WParam) & 1) != 0)
			{
				bool flag6 = num == 2;
				if (flag6 != flag2 || !flag4)
				{
					uICues |= UICues.ChangeFocus;
					this.uiCuesState &= -16;
					this.uiCuesState |= (flag6 ? 2 : 1);
				}
				if (flag6)
				{
					uICues |= UICues.ShowFocus;
				}
			}
			if ((uICues & UICues.Changed) != UICues.None)
			{
				this.OnChangeUICues(new UICuesEventArgs(uICues));
				this.Invalidate(true);
			}
		}

		private unsafe void WmWindowPosChanged(ref Message m)
		{
			this.DefWndProc(ref m);
			this.UpdateBounds();
			if (this.parent != null && UnsafeNativeMethods.GetParent(new HandleRef(this.window, this.InternalHandle)) == this.parent.InternalHandle && (this.state & 256) == 0)
			{
				NativeMethods.WINDOWPOS* ptr = (NativeMethods.WINDOWPOS*)((void*)m.LParam);
				if ((ptr->flags & 4) == 0)
				{
					this.parent.UpdateChildControlIndex(this);
				}
			}
		}

		/// <summary>Обрабатывает сообщения Windows.</summary>
		/// <param name="m">Сообщение <see cref="T:System.Windows.Forms.Message" /> Windows для обработки. </param>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode), SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		protected virtual void WndProc(ref Message m)
		{
			if ((this.controlStyle & ControlStyles.EnableNotifyMessage) == ControlStyles.EnableNotifyMessage)
			{
				this.OnNotifyMessage(m);
			}
			int msg = m.Msg;
			if (msg <= 126)
			{
				if (msg <= 32)
				{
					if (msg <= 16)
					{
						switch (msg)
						{
						case 1:
							this.WmCreate(ref m);
							return;
						case 2:
							this.WmDestroy(ref m);
							return;
						case 3:
							this.WmMove(ref m);
							return;
						case 4:
						case 5:
						case 6:
							goto IL_640;
						case 7:
							this.WmSetFocus(ref m);
							return;
						case 8:
							this.WmKillFocus(ref m);
							return;
						default:
							if (msg != 15)
							{
								if (msg != 16)
								{
									goto IL_640;
								}
								this.WmClose(ref m);
								return;
							}
							else
							{
								if (this.GetStyle(ControlStyles.UserPaint))
								{
									this.WmPaint(ref m);
									return;
								}
								this.DefWndProc(ref m);
								return;
							}
							break;
						}
					}
					else if (msg <= 24)
					{
						if (msg == 20)
						{
							this.WmEraseBkgnd(ref m);
							return;
						}
						if (msg != 24)
						{
							goto IL_640;
						}
						this.WmShowWindow(ref m);
						return;
					}
					else if (msg != 25)
					{
						if (msg != 32)
						{
							goto IL_640;
						}
						this.WmSetCursor(ref m);
						return;
					}
				}
				else if (msg <= 70)
				{
					if (msg <= 57)
					{
						switch (msg)
						{
						case 43:
							this.WmDrawItem(ref m);
							return;
						case 44:
							this.WmMeasureItem(ref m);
							return;
						case 45:
						case 46:
						case 47:
							goto IL_443;
						default:
							if (msg != 57)
							{
								goto IL_640;
							}
							goto IL_443;
						}
					}
					else
					{
						if (msg == 61)
						{
							this.WmGetObject(ref m);
							return;
						}
						if (msg != 70)
						{
							goto IL_640;
						}
						this.WmWindowPosChanging(ref m);
						return;
					}
				}
				else if (msg <= 85)
				{
					if (msg == 71)
					{
						this.WmWindowPosChanged(ref m);
						return;
					}
					switch (msg)
					{
					case 78:
						this.WmNotify(ref m);
						return;
					case 79:
					case 82:
					case 84:
						goto IL_640;
					case 80:
						this.WmInputLangChangeRequest(ref m);
						return;
					case 81:
						this.WmInputLangChange(ref m);
						return;
					case 83:
						this.WmHelp(ref m);
						return;
					case 85:
						this.WmNotifyFormat(ref m);
						return;
					default:
						goto IL_640;
					}
				}
				else
				{
					if (msg == 123)
					{
						this.WmContextMenu(ref m);
						return;
					}
					if (msg != 126)
					{
						goto IL_640;
					}
					this.WmDisplayChange(ref m);
					return;
				}
			}
			else if (msg <= 642)
			{
				if (msg <= 288)
				{
					if (msg <= 279)
					{
						switch (msg)
						{
						case 256:
						case 257:
						case 258:
						case 260:
						case 261:
							this.WmKeyChar(ref m);
							return;
						case 259:
							goto IL_640;
						default:
							switch (msg)
							{
							case 269:
								this.WmImeStartComposition(ref m);
								return;
							case 270:
								this.WmImeEndComposition(ref m);
								return;
							case 271:
							case 272:
							case 275:
							case 278:
								goto IL_640;
							case 273:
								this.WmCommand(ref m);
								return;
							case 274:
								if (((int)((long)m.WParam) & 65520) == 61696 && ToolStripManager.ProcessMenuKey(ref m))
								{
									m.Result = IntPtr.Zero;
									return;
								}
								this.DefWndProc(ref m);
								return;
							case 276:
							case 277:
								goto IL_443;
							case 279:
								this.WmInitMenuPopup(ref m);
								return;
							default:
								goto IL_640;
							}
							break;
						}
					}
					else
					{
						if (msg == 287)
						{
							this.WmMenuSelect(ref m);
							return;
						}
						if (msg != 288)
						{
							goto IL_640;
						}
						this.WmMenuChar(ref m);
						return;
					}
				}
				else if (msg <= 312)
				{
					if (msg == 296)
					{
						this.WmUpdateUIState(ref m);
						return;
					}
					switch (msg)
					{
					case 306:
					case 307:
					case 308:
					case 309:
					case 310:
					case 311:
					case 312:
						break;
					default:
						goto IL_640;
					}
				}
				else
				{
					switch (msg)
					{
					case 512:
						this.WmMouseMove(ref m);
						return;
					case 513:
						this.WmMouseDown(ref m, MouseButtons.Left, 1);
						return;
					case 514:
						this.WmMouseUp(ref m, MouseButtons.Left, 1);
						return;
					case 515:
						this.WmMouseDown(ref m, MouseButtons.Left, 2);
						if (this.GetStyle(ControlStyles.StandardDoubleClick))
						{
							this.SetState(67108864, true);
							return;
						}
						return;
					case 516:
						this.WmMouseDown(ref m, MouseButtons.Right, 1);
						return;
					case 517:
						this.WmMouseUp(ref m, MouseButtons.Right, 1);
						return;
					case 518:
						this.WmMouseDown(ref m, MouseButtons.Right, 2);
						if (this.GetStyle(ControlStyles.StandardDoubleClick))
						{
							this.SetState(67108864, true);
							return;
						}
						return;
					case 519:
						this.WmMouseDown(ref m, MouseButtons.Middle, 1);
						return;
					case 520:
						this.WmMouseUp(ref m, MouseButtons.Middle, 1);
						return;
					case 521:
						this.WmMouseDown(ref m, MouseButtons.Middle, 2);
						if (this.GetStyle(ControlStyles.StandardDoubleClick))
						{
							this.SetState(67108864, true);
							return;
						}
						return;
					case 522:
						this.WmMouseWheel(ref m);
						return;
					case 523:
						this.WmMouseDown(ref m, this.GetXButton(NativeMethods.Util.HIWORD(m.WParam)), 1);
						return;
					case 524:
						this.WmMouseUp(ref m, this.GetXButton(NativeMethods.Util.HIWORD(m.WParam)), 1);
						return;
					case 525:
						this.WmMouseDown(ref m, this.GetXButton(NativeMethods.Util.HIWORD(m.WParam)), 2);
						if (this.GetStyle(ControlStyles.StandardDoubleClick))
						{
							this.SetState(67108864, true);
							return;
						}
						return;
					case 526:
					case 527:
					case 529:
					case 531:
					case 532:
						goto IL_640;
					case 528:
						this.WmParentNotify(ref m);
						return;
					case 530:
						this.WmExitMenuLoop(ref m);
						return;
					case 533:
						this.WmCaptureChanged(ref m);
						return;
					default:
						if (msg != 642)
						{
							goto IL_640;
						}
						this.WmImeNotify(ref m);
						return;
					}
				}
			}
			else if (msg <= 783)
			{
				if (msg <= 673)
				{
					if (msg == 646)
					{
						this.WmImeChar(ref m);
						return;
					}
					if (msg != 673)
					{
						goto IL_640;
					}
					this.WmMouseHover(ref m);
					return;
				}
				else
				{
					if (msg == 675)
					{
						this.WmMouseLeave(ref m);
						return;
					}
					if (msg != 783)
					{
						goto IL_640;
					}
					this.WmQueryNewPalette(ref m);
					return;
				}
			}
			else if (msg <= 8217)
			{
				if (msg != 792)
				{
					if (msg != 8217)
					{
						goto IL_640;
					}
				}
				else
				{
					if (this.GetStyle(ControlStyles.UserPaint))
					{
						this.WmPrintClient(ref m);
						return;
					}
					this.DefWndProc(ref m);
					return;
				}
			}
			else
			{
				if (msg == 8277)
				{
					m.Result = (IntPtr)((Marshal.SystemDefaultCharSize == 1) ? 1 : 2);
					return;
				}
				switch (msg)
				{
				case 8498:
				case 8499:
				case 8500:
				case 8501:
				case 8502:
				case 8503:
				case 8504:
					break;
				default:
					goto IL_640;
				}
			}
			this.WmCtlColorControl(ref m);
			return;
			IL_443:
			if (!Control.ReflectMessageInternal(m.LParam, ref m))
			{
				this.DefWndProc(ref m);
				return;
			}
			return;
			IL_640:
			if (m.Msg == Control.threadCallbackMessage && m.Msg != 0)
			{
				this.InvokeMarshaledCallbacks();
				return;
			}
			if (m.Msg == Control.WM_GETCONTROLNAME)
			{
				this.WmGetControlName(ref m);
				return;
			}
			if (m.Msg == Control.WM_GETCONTROLTYPE)
			{
				this.WmGetControlType(ref m);
				return;
			}
			if (Control.mouseWheelRoutingNeeded && m.Msg == Control.mouseWheelMessage)
			{
				Keys keys = Keys.None;
				keys |= ((UnsafeNativeMethods.GetKeyState(17) < 0) ? Keys.Back : Keys.None);
				keys |= ((UnsafeNativeMethods.GetKeyState(16) < 0) ? Keys.MButton : Keys.None);
				IntPtr focus = UnsafeNativeMethods.GetFocus();
				if (focus == IntPtr.Zero)
				{
					this.SendMessage(m.Msg, (IntPtr)((int)((long)m.WParam) << 16 | (int)keys), m.LParam);
				}
				else
				{
					IntPtr value = IntPtr.Zero;
					IntPtr desktopWindow = UnsafeNativeMethods.GetDesktopWindow();
					while (value == IntPtr.Zero && focus != IntPtr.Zero && focus != desktopWindow)
					{
						value = UnsafeNativeMethods.SendMessage(new HandleRef(null, focus), 522, (int)((long)m.WParam) << 16 | (int)keys, m.LParam);
						focus = UnsafeNativeMethods.GetParent(new HandleRef(null, focus));
					}
				}
			}
			if (m.Msg == NativeMethods.WM_MOUSEENTER)
			{
				this.WmMouseEnter(ref m);
				return;
			}
			this.DefWndProc(ref m);
		}

		private void WndProcException(Exception e)
		{
			Application.OnThreadException(e);
		}

		void IArrangedElement.PerformLayout(IArrangedElement affectedElement, string affectedProperty)
		{
			this.PerformLayout(new LayoutEventArgs(affectedElement, affectedProperty));
		}

		void IArrangedElement.SetBounds(Rectangle bounds, BoundsSpecified specified)
		{
			ISite site = this.Site;
			IComponentChangeService componentChangeService = null;
			PropertyDescriptor propertyDescriptor = null;
			PropertyDescriptor propertyDescriptor2 = null;
			bool flag = false;
			bool flag2 = false;
			if (site != null && site.DesignMode)
			{
				componentChangeService = (IComponentChangeService)site.GetService(typeof(IComponentChangeService));
				if (componentChangeService != null)
				{
					propertyDescriptor = TypeDescriptor.GetProperties(this)[PropertyNames.Size];
					propertyDescriptor2 = TypeDescriptor.GetProperties(this)[PropertyNames.Location];
					try
					{
						if (propertyDescriptor != null && !propertyDescriptor.IsReadOnly && (bounds.Width != this.Width || bounds.Height != this.Height))
						{
							if (!(site is INestedSite))
							{
								componentChangeService.OnComponentChanging(this, propertyDescriptor);
							}
							flag = true;
						}
						if (propertyDescriptor2 != null && !propertyDescriptor2.IsReadOnly && (bounds.X != this.x || bounds.Y != this.y))
						{
							if (!(site is INestedSite))
							{
								componentChangeService.OnComponentChanging(this, propertyDescriptor2);
							}
							flag2 = true;
						}
					}
					catch (InvalidOperationException)
					{
					}
				}
			}
			this.SetBoundsCore(bounds.X, bounds.Y, bounds.Width, bounds.Height, specified);
			if (site != null && componentChangeService != null)
			{
				try
				{
					if (flag)
					{
						componentChangeService.OnComponentChanged(this, propertyDescriptor, null, null);
					}
					if (flag2)
					{
						componentChangeService.OnComponentChanged(this, propertyDescriptor2, null, null);
					}
				}
				catch (InvalidOperationException)
				{
				}
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.DragEnter" />.</summary>
		/// <param name="drgEvent">Объект <see cref="T:System.Windows.Forms.DragEventArgs" />, содержащий данные, относящиеся к событию. </param>
		void IDropTarget.OnDragEnter(DragEventArgs drgEvent)
		{
			this.OnDragEnter(drgEvent);
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.DragOver" />.</summary>
		/// <param name="drgEvent">Объект <see cref="T:System.Windows.Forms.DragEventArgs" />, содержащий данные, относящиеся к событию. </param>
		void IDropTarget.OnDragOver(DragEventArgs drgEvent)
		{
			this.OnDragOver(drgEvent);
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.DragLeave" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные, относящиеся к событию. </param>
		void IDropTarget.OnDragLeave(EventArgs e)
		{
			this.OnDragLeave(e);
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.DragDrop" />.</summary>
		/// <param name="drgEvent">Объект <see cref="T:System.Windows.Forms.DragEventArgs" />, содержащий данные, относящиеся к событию. </param>
		void IDropTarget.OnDragDrop(DragEventArgs drgEvent)
		{
			this.OnDragDrop(drgEvent);
		}

		void ISupportOleDropSource.OnGiveFeedback(GiveFeedbackEventArgs giveFeedbackEventArgs)
		{
			this.OnGiveFeedback(giveFeedbackEventArgs);
		}

		void ISupportOleDropSource.OnQueryContinueDrag(QueryContinueDragEventArgs queryContinueDragEventArgs)
		{
			this.OnQueryContinueDrag(queryContinueDragEventArgs);
		}

		int UnsafeNativeMethods.IOleControl.GetControlInfo(NativeMethods.tagCONTROLINFO pCI)
		{
			pCI.cb = Marshal.SizeOf(typeof(NativeMethods.tagCONTROLINFO));
			pCI.hAccel = IntPtr.Zero;
			pCI.cAccel = 0;
			pCI.dwFlags = 0;
			if (this.IsInputKey(Keys.Return))
			{
				pCI.dwFlags |= 1;
			}
			if (this.IsInputKey(Keys.Escape))
			{
				pCI.dwFlags |= 2;
			}
			this.ActiveXInstance.GetControlInfo(pCI);
			return 0;
		}

		int UnsafeNativeMethods.IOleControl.OnMnemonic(ref NativeMethods.MSG pMsg)
		{
			this.ProcessMnemonic((char)((int)pMsg.wParam));
			return 0;
		}

		int UnsafeNativeMethods.IOleControl.OnAmbientPropertyChange(int dispID)
		{
			this.ActiveXInstance.OnAmbientPropertyChange(dispID);
			return 0;
		}

		int UnsafeNativeMethods.IOleControl.FreezeEvents(int bFreeze)
		{
			this.ActiveXInstance.EventsFrozen = (bFreeze != 0);
			return 0;
		}

		int UnsafeNativeMethods.IOleInPlaceActiveObject.GetWindow(out IntPtr hwnd)
		{
			return ((UnsafeNativeMethods.IOleInPlaceObject)this).GetWindow(out hwnd);
		}

		void UnsafeNativeMethods.IOleInPlaceActiveObject.ContextSensitiveHelp(int fEnterMode)
		{
			((UnsafeNativeMethods.IOleInPlaceObject)this).ContextSensitiveHelp(fEnterMode);
		}

		int UnsafeNativeMethods.IOleInPlaceActiveObject.TranslateAccelerator(ref NativeMethods.MSG lpmsg)
		{
			return this.ActiveXInstance.TranslateAccelerator(ref lpmsg);
		}

		void UnsafeNativeMethods.IOleInPlaceActiveObject.OnFrameWindowActivate(bool fActivate)
		{
			this.OnFrameWindowActivate(fActivate);
		}

		void UnsafeNativeMethods.IOleInPlaceActiveObject.OnDocWindowActivate(int fActivate)
		{
			this.ActiveXInstance.OnDocWindowActivate(fActivate);
		}

		void UnsafeNativeMethods.IOleInPlaceActiveObject.ResizeBorder(NativeMethods.COMRECT prcBorder, UnsafeNativeMethods.IOleInPlaceUIWindow pUIWindow, bool fFrameWindow)
		{
		}

		void UnsafeNativeMethods.IOleInPlaceActiveObject.EnableModeless(int fEnable)
		{
		}

		int UnsafeNativeMethods.IOleInPlaceObject.GetWindow(out IntPtr hwnd)
		{
			return this.ActiveXInstance.GetWindow(out hwnd);
		}

		void UnsafeNativeMethods.IOleInPlaceObject.ContextSensitiveHelp(int fEnterMode)
		{
			if (fEnterMode != 0)
			{
				this.OnHelpRequested(new HelpEventArgs(Control.MousePosition));
			}
		}

		void UnsafeNativeMethods.IOleInPlaceObject.InPlaceDeactivate()
		{
			this.ActiveXInstance.InPlaceDeactivate();
		}

		int UnsafeNativeMethods.IOleInPlaceObject.UIDeactivate()
		{
			return this.ActiveXInstance.UIDeactivate();
		}

		void UnsafeNativeMethods.IOleInPlaceObject.SetObjectRects(NativeMethods.COMRECT lprcPosRect, NativeMethods.COMRECT lprcClipRect)
		{
			this.ActiveXInstance.SetObjectRects(lprcPosRect, lprcClipRect);
		}

		void UnsafeNativeMethods.IOleInPlaceObject.ReactivateAndUndo()
		{
		}

		int UnsafeNativeMethods.IOleObject.SetClientSite(UnsafeNativeMethods.IOleClientSite pClientSite)
		{
			this.ActiveXInstance.SetClientSite(pClientSite);
			return 0;
		}

		UnsafeNativeMethods.IOleClientSite UnsafeNativeMethods.IOleObject.GetClientSite()
		{
			return this.ActiveXInstance.GetClientSite();
		}

		int UnsafeNativeMethods.IOleObject.SetHostNames(string szContainerApp, string szContainerObj)
		{
			return 0;
		}

		int UnsafeNativeMethods.IOleObject.Close(int dwSaveOption)
		{
			this.ActiveXInstance.Close(dwSaveOption);
			return 0;
		}

		int UnsafeNativeMethods.IOleObject.SetMoniker(int dwWhichMoniker, object pmk)
		{
			return -2147467263;
		}

		int UnsafeNativeMethods.IOleObject.GetMoniker(int dwAssign, int dwWhichMoniker, out object moniker)
		{
			moniker = null;
			return -2147467263;
		}

		int UnsafeNativeMethods.IOleObject.InitFromData(System.Runtime.InteropServices.ComTypes.IDataObject pDataObject, int fCreation, int dwReserved)
		{
			return -2147467263;
		}

		int UnsafeNativeMethods.IOleObject.GetClipboardData(int dwReserved, out System.Runtime.InteropServices.ComTypes.IDataObject data)
		{
			data = null;
			return -2147467263;
		}

		int UnsafeNativeMethods.IOleObject.DoVerb(int iVerb, IntPtr lpmsg, UnsafeNativeMethods.IOleClientSite pActiveSite, int lindex, IntPtr hwndParent, NativeMethods.COMRECT lprcPosRect)
		{
			iVerb = (int)((short)iVerb);
			try
			{
				this.ActiveXInstance.DoVerb(iVerb, lpmsg, pActiveSite, lindex, hwndParent, lprcPosRect);
			}
			catch (Exception)
			{
				throw;
			}
			finally
			{
			}
			return 0;
		}

		int UnsafeNativeMethods.IOleObject.EnumVerbs(out UnsafeNativeMethods.IEnumOLEVERB e)
		{
			return Control.ActiveXImpl.EnumVerbs(out e);
		}

		int UnsafeNativeMethods.IOleObject.OleUpdate()
		{
			return 0;
		}

		int UnsafeNativeMethods.IOleObject.IsUpToDate()
		{
			return 0;
		}

		int UnsafeNativeMethods.IOleObject.GetUserClassID(ref Guid pClsid)
		{
			pClsid = base.GetType().GUID;
			return 0;
		}

		int UnsafeNativeMethods.IOleObject.GetUserType(int dwFormOfType, out string userType)
		{
			if (dwFormOfType == 1)
			{
				userType = base.GetType().FullName;
			}
			else
			{
				userType = base.GetType().Name;
			}
			return 0;
		}

		int UnsafeNativeMethods.IOleObject.SetExtent(int dwDrawAspect, NativeMethods.tagSIZEL pSizel)
		{
			this.ActiveXInstance.SetExtent(dwDrawAspect, pSizel);
			return 0;
		}

		int UnsafeNativeMethods.IOleObject.GetExtent(int dwDrawAspect, NativeMethods.tagSIZEL pSizel)
		{
			this.ActiveXInstance.GetExtent(dwDrawAspect, pSizel);
			return 0;
		}

		int UnsafeNativeMethods.IOleObject.Advise(IAdviseSink pAdvSink, out int cookie)
		{
			cookie = this.ActiveXInstance.Advise(pAdvSink);
			return 0;
		}

		int UnsafeNativeMethods.IOleObject.Unadvise(int dwConnection)
		{
			this.ActiveXInstance.Unadvise(dwConnection);
			return 0;
		}

		int UnsafeNativeMethods.IOleObject.EnumAdvise(out IEnumSTATDATA e)
		{
			e = null;
			return -2147467263;
		}

		int UnsafeNativeMethods.IOleObject.GetMiscStatus(int dwAspect, out int cookie)
		{
			if ((dwAspect & 1) != 0)
			{
				int num = 131456;
				if (this.GetStyle(ControlStyles.ResizeRedraw))
				{
					num |= 1;
				}
				if (this is IButtonControl)
				{
					num |= 4096;
				}
				cookie = num;
				return 0;
			}
			cookie = 0;
			return -2147221397;
		}

		int UnsafeNativeMethods.IOleObject.SetColorScheme(NativeMethods.tagLOGPALETTE pLogpal)
		{
			return 0;
		}

		int UnsafeNativeMethods.IOleWindow.GetWindow(out IntPtr hwnd)
		{
			return ((UnsafeNativeMethods.IOleInPlaceObject)this).GetWindow(out hwnd);
		}

		void UnsafeNativeMethods.IOleWindow.ContextSensitiveHelp(int fEnterMode)
		{
			((UnsafeNativeMethods.IOleInPlaceObject)this).ContextSensitiveHelp(fEnterMode);
		}

		void UnsafeNativeMethods.IPersist.GetClassID(out Guid pClassID)
		{
			pClassID = base.GetType().GUID;
		}

		void UnsafeNativeMethods.IPersistPropertyBag.InitNew()
		{
		}

		void UnsafeNativeMethods.IPersistPropertyBag.GetClassID(out Guid pClassID)
		{
			pClassID = base.GetType().GUID;
		}

		void UnsafeNativeMethods.IPersistPropertyBag.Load(UnsafeNativeMethods.IPropertyBag pPropBag, UnsafeNativeMethods.IErrorLog pErrorLog)
		{
			this.ActiveXInstance.Load(pPropBag, pErrorLog);
		}

		void UnsafeNativeMethods.IPersistPropertyBag.Save(UnsafeNativeMethods.IPropertyBag pPropBag, bool fClearDirty, bool fSaveAllProperties)
		{
			this.ActiveXInstance.Save(pPropBag, fClearDirty, fSaveAllProperties);
		}

		void UnsafeNativeMethods.IPersistStorage.GetClassID(out Guid pClassID)
		{
			pClassID = base.GetType().GUID;
		}

		int UnsafeNativeMethods.IPersistStorage.IsDirty()
		{
			return this.ActiveXInstance.IsDirty();
		}

		void UnsafeNativeMethods.IPersistStorage.InitNew(UnsafeNativeMethods.IStorage pstg)
		{
		}

		int UnsafeNativeMethods.IPersistStorage.Load(UnsafeNativeMethods.IStorage pstg)
		{
			this.ActiveXInstance.Load(pstg);
			return 0;
		}

		void UnsafeNativeMethods.IPersistStorage.Save(UnsafeNativeMethods.IStorage pstg, bool fSameAsLoad)
		{
			this.ActiveXInstance.Save(pstg, fSameAsLoad);
		}

		void UnsafeNativeMethods.IPersistStorage.SaveCompleted(UnsafeNativeMethods.IStorage pStgNew)
		{
		}

		void UnsafeNativeMethods.IPersistStorage.HandsOffStorage()
		{
		}

		void UnsafeNativeMethods.IPersistStreamInit.GetClassID(out Guid pClassID)
		{
			pClassID = base.GetType().GUID;
		}

		int UnsafeNativeMethods.IPersistStreamInit.IsDirty()
		{
			return this.ActiveXInstance.IsDirty();
		}

		void UnsafeNativeMethods.IPersistStreamInit.Load(UnsafeNativeMethods.IStream pstm)
		{
			this.ActiveXInstance.Load(pstm);
		}

		void UnsafeNativeMethods.IPersistStreamInit.Save(UnsafeNativeMethods.IStream pstm, bool fClearDirty)
		{
			this.ActiveXInstance.Save(pstm, fClearDirty);
		}

		void UnsafeNativeMethods.IPersistStreamInit.GetSizeMax(long pcbSize)
		{
		}

		void UnsafeNativeMethods.IPersistStreamInit.InitNew()
		{
		}

		void UnsafeNativeMethods.IQuickActivate.QuickActivate(UnsafeNativeMethods.tagQACONTAINER pQaContainer, UnsafeNativeMethods.tagQACONTROL pQaControl)
		{
			this.ActiveXInstance.QuickActivate(pQaContainer, pQaControl);
		}

		void UnsafeNativeMethods.IQuickActivate.SetContentExtent(NativeMethods.tagSIZEL pSizel)
		{
			this.ActiveXInstance.SetExtent(1, pSizel);
		}

		void UnsafeNativeMethods.IQuickActivate.GetContentExtent(NativeMethods.tagSIZEL pSizel)
		{
			this.ActiveXInstance.GetExtent(1, pSizel);
		}

		int UnsafeNativeMethods.IViewObject.Draw(int dwDrawAspect, int lindex, IntPtr pvAspect, NativeMethods.tagDVTARGETDEVICE ptd, IntPtr hdcTargetDev, IntPtr hdcDraw, NativeMethods.COMRECT lprcBounds, NativeMethods.COMRECT lprcWBounds, IntPtr pfnContinue, int dwContinue)
		{
			try
			{
				this.ActiveXInstance.Draw(dwDrawAspect, lindex, pvAspect, ptd, hdcTargetDev, hdcDraw, lprcBounds, lprcWBounds, pfnContinue, dwContinue);
			}
			catch (ExternalException arg_1E_0)
			{
				return arg_1E_0.ErrorCode;
			}
			finally
			{
			}
			return 0;
		}

		int UnsafeNativeMethods.IViewObject.GetColorSet(int dwDrawAspect, int lindex, IntPtr pvAspect, NativeMethods.tagDVTARGETDEVICE ptd, IntPtr hicTargetDev, NativeMethods.tagLOGPALETTE ppColorSet)
		{
			return -2147467263;
		}

		int UnsafeNativeMethods.IViewObject.Freeze(int dwDrawAspect, int lindex, IntPtr pvAspect, IntPtr pdwFreeze)
		{
			return -2147467263;
		}

		int UnsafeNativeMethods.IViewObject.Unfreeze(int dwFreeze)
		{
			return -2147467263;
		}

		void UnsafeNativeMethods.IViewObject.SetAdvise(int aspects, int advf, IAdviseSink pAdvSink)
		{
			this.ActiveXInstance.SetAdvise(aspects, advf, pAdvSink);
		}

		void UnsafeNativeMethods.IViewObject.GetAdvise(int[] paspects, int[] padvf, IAdviseSink[] pAdvSink)
		{
			this.ActiveXInstance.GetAdvise(paspects, padvf, pAdvSink);
		}

		void UnsafeNativeMethods.IViewObject2.Draw(int dwDrawAspect, int lindex, IntPtr pvAspect, NativeMethods.tagDVTARGETDEVICE ptd, IntPtr hdcTargetDev, IntPtr hdcDraw, NativeMethods.COMRECT lprcBounds, NativeMethods.COMRECT lprcWBounds, IntPtr pfnContinue, int dwContinue)
		{
			this.ActiveXInstance.Draw(dwDrawAspect, lindex, pvAspect, ptd, hdcTargetDev, hdcDraw, lprcBounds, lprcWBounds, pfnContinue, dwContinue);
		}

		int UnsafeNativeMethods.IViewObject2.GetColorSet(int dwDrawAspect, int lindex, IntPtr pvAspect, NativeMethods.tagDVTARGETDEVICE ptd, IntPtr hicTargetDev, NativeMethods.tagLOGPALETTE ppColorSet)
		{
			return -2147467263;
		}

		int UnsafeNativeMethods.IViewObject2.Freeze(int dwDrawAspect, int lindex, IntPtr pvAspect, IntPtr pdwFreeze)
		{
			return -2147467263;
		}

		int UnsafeNativeMethods.IViewObject2.Unfreeze(int dwFreeze)
		{
			return -2147467263;
		}

		void UnsafeNativeMethods.IViewObject2.SetAdvise(int aspects, int advf, IAdviseSink pAdvSink)
		{
			this.ActiveXInstance.SetAdvise(aspects, advf, pAdvSink);
		}

		void UnsafeNativeMethods.IViewObject2.GetAdvise(int[] paspects, int[] padvf, IAdviseSink[] pAdvSink)
		{
			this.ActiveXInstance.GetAdvise(paspects, padvf, pAdvSink);
		}

		void UnsafeNativeMethods.IViewObject2.GetExtent(int dwDrawAspect, int lindex, NativeMethods.tagDVTARGETDEVICE ptd, NativeMethods.tagSIZEL lpsizel)
		{
			((UnsafeNativeMethods.IOleObject)this).GetExtent(dwDrawAspect, lpsizel);
		}

		internal void UpdateImeContextMode()
		{
			ImeMode[] inputLanguageTable = ImeModeConversion.InputLanguageTable;
			if (!base.DesignMode && inputLanguageTable != ImeModeConversion.UnsupportedTable && this.Focused)
			{
				ImeMode imeMode = ImeMode.Disable;
				ImeMode cachedImeMode = this.CachedImeMode;
				if (this.ImeSupported && this.CanEnableIme)
				{
					imeMode = ((cachedImeMode == ImeMode.NoControl) ? Control.PropagatingImeMode : cachedImeMode);
				}
				if (this.CurrentImeContextMode != imeMode && imeMode != ImeMode.Inherit)
				{
					int disableImeModeChangedCount = this.DisableImeModeChangedCount;
					this.DisableImeModeChangedCount = disableImeModeChangedCount + 1;
					ImeMode imeMode2 = Control.PropagatingImeMode;
					try
					{
						ImeContext.SetImeStatus(imeMode, this.Handle);
					}
					finally
					{
						disableImeModeChangedCount = this.DisableImeModeChangedCount;
						this.DisableImeModeChangedCount = disableImeModeChangedCount - 1;
						if (imeMode == ImeMode.Disable && inputLanguageTable == ImeModeConversion.ChineseTable)
						{
							Control.PropagatingImeMode = imeMode2;
						}
					}
					if (cachedImeMode == ImeMode.NoControl)
					{
						if (this.CanEnableIme)
						{
							Control.PropagatingImeMode = this.CurrentImeContextMode;
							return;
						}
					}
					else
					{
						if (this.CanEnableIme)
						{
							this.CachedImeMode = this.CurrentImeContextMode;
						}
						this.VerifyImeModeChanged(imeMode, this.CachedImeMode);
					}
				}
			}
		}

		private void VerifyImeModeChanged(ImeMode oldMode, ImeMode newMode)
		{
			if (this.ImeSupported && this.DisableImeModeChangedCount == 0 && newMode != ImeMode.NoControl && oldMode != newMode)
			{
				this.OnImeModeChanged(EventArgs.Empty);
			}
		}

		internal void VerifyImeRestrictedModeChanged()
		{
			bool canEnableIme = this.CanEnableIme;
			if (this.LastCanEnableIme != canEnableIme)
			{
				if (this.Focused)
				{
					int disableImeModeChangedCount = this.DisableImeModeChangedCount;
					this.DisableImeModeChangedCount = disableImeModeChangedCount + 1;
					try
					{
						this.UpdateImeContextMode();
					}
					finally
					{
						disableImeModeChangedCount = this.DisableImeModeChangedCount;
						this.DisableImeModeChangedCount = disableImeModeChangedCount - 1;
					}
				}
				ImeMode imeMode = this.CachedImeMode;
				ImeMode newMode = ImeMode.Disable;
				if (canEnableIme)
				{
					newMode = imeMode;
					imeMode = ImeMode.Disable;
				}
				this.VerifyImeModeChanged(imeMode, newMode);
				this.LastCanEnableIme = canEnableIme;
			}
		}

		internal void OnImeContextStatusChanged(IntPtr handle)
		{
			ImeMode imeMode = ImeContext.GetImeMode(handle);
			if (imeMode != ImeMode.Inherit)
			{
				ImeMode cachedImeMode = this.CachedImeMode;
				if (this.CanEnableIme)
				{
					if (cachedImeMode != ImeMode.NoControl)
					{
						this.CachedImeMode = imeMode;
						this.VerifyImeModeChanged(cachedImeMode, this.CachedImeMode);
						return;
					}
					Control.PropagatingImeMode = imeMode;
				}
			}
		}

		/// <summary>Вызывает событие <see cref="E:System.Windows.Forms.Control.ImeModeChanged" />.</summary>
		/// <param name="e">Объект <see cref="T:System.EventArgs" />, содержащий данные события. </param>
		protected virtual void OnImeModeChanged(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[Control.EventImeModeChanged];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		/// <summary>Сбрасывает свойство <see cref="P:System.Windows.Forms.Control.ImeMode" /> в значение по умолчанию.</summary>
		/// <filterpriority>2</filterpriority>
		/// <PermissionSet>
		///   <IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" />
		///   <IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" />
		/// </PermissionSet>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void ResetImeMode()
		{
			this.ImeMode = this.DefaultImeMode;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal virtual bool ShouldSerializeImeMode()
		{
			bool flag;
			int integer = this.Properties.GetInteger(Control.PropImeMode, out flag);
			return flag && integer != (int)this.DefaultImeMode;
		}

		private void WmInputLangChange(ref Message m)
		{
			this.UpdateImeContextMode();
			if (ImeModeConversion.InputLanguageTable == ImeModeConversion.UnsupportedTable)
			{
				Control.PropagatingImeMode = ImeMode.Off;
			}
			if (ImeModeConversion.InputLanguageTable == ImeModeConversion.ChineseTable)
			{
				Control.IgnoreWmImeNotify = false;
			}
			Form form = this.FindFormInternal();
			if (form != null)
			{
				InputLanguageChangedEventArgs iplevent = InputLanguage.CreateInputLanguageChangedEventArgs(m);
				form.PerformOnInputLanguageChanged(iplevent);
			}
			this.DefWndProc(ref m);
		}

		private void WmInputLangChangeRequest(ref Message m)
		{
			InputLanguageChangingEventArgs inputLanguageChangingEventArgs = InputLanguage.CreateInputLanguageChangingEventArgs(m);
			Form form = this.FindFormInternal();
			if (form != null)
			{
				form.PerformOnInputLanguageChanging(inputLanguageChangingEventArgs);
			}
			if (!inputLanguageChangingEventArgs.Cancel)
			{
				this.DefWndProc(ref m);
				return;
			}
			m.Result = IntPtr.Zero;
		}

		private void WmImeChar(ref Message m)
		{
			if (this.ProcessKeyEventArgs(ref m))
			{
				return;
			}
			this.DefWndProc(ref m);
		}

		private void WmImeEndComposition(ref Message m)
		{
			this.ImeWmCharsToIgnore = -1;
			this.DefWndProc(ref m);
		}

		private void WmImeNotify(ref Message m)
		{
			ImeMode[] inputLanguageTable = ImeModeConversion.InputLanguageTable;
			if (inputLanguageTable == ImeModeConversion.ChineseTable && !Control.lastLanguageChinese)
			{
				Control.IgnoreWmImeNotify = true;
			}
			Control.lastLanguageChinese = (inputLanguageTable == ImeModeConversion.ChineseTable);
			if (this.ImeSupported && inputLanguageTable != ImeModeConversion.UnsupportedTable && !Control.IgnoreWmImeNotify)
			{
				int num = (int)m.WParam;
				if (num == 6 || num == 8)
				{
					this.OnImeContextStatusChanged(this.Handle);
				}
			}
			this.DefWndProc(ref m);
		}

		internal void WmImeSetFocus()
		{
			if (ImeModeConversion.InputLanguageTable != ImeModeConversion.UnsupportedTable)
			{
				this.UpdateImeContextMode();
			}
		}

		private void WmImeStartComposition(ref Message m)
		{
			this.Properties.SetInteger(Control.PropImeWmCharsToIgnore, 0);
			this.DefWndProc(ref m);
		}

		private void WmImeKillFocus()
		{
			Control topMostParent = this.TopMostParent;
			Form form = topMostParent as Form;
			if ((form == null || form.Modal) && !topMostParent.ContainsFocus && Control.propagatingImeMode != ImeMode.Inherit)
			{
				Control.IgnoreWmImeNotify = true;
				try
				{
					ImeContext.SetImeStatus(Control.PropagatingImeMode, topMostParent.Handle);
					Control.PropagatingImeMode = ImeMode.Inherit;
				}
				finally
				{
					Control.IgnoreWmImeNotify = false;
				}
			}
		}
	}
}
