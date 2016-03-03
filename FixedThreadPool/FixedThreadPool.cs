using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FixedThreadPool
{
    /// <summary>
    /// Приоритет выполнения задачи
    /// </summary>
    public enum Priority
    {
        HIGH,
        NORMAL,
        LOW
    }

    public class FixedThreadPool
    {
        /// <summary>
        /// Количество рабочих потоков
        /// </summary>
        int m_WorkerCount;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a_WorkerCount">Количество потоков в пуле</param>
        public FixedThreadPool(int a_WorkerCount)
        {
            m_WorkerCount = a_WorkerCount;
        }

        #region Реализация

        /// <summary>
        /// Объект для блокировки
        /// </summary>
        object m_Lock = new object();

        /// <summary>
        /// Рабочие потоки
        /// </summary>
        Thread[] m_Threads = null;

        /// <summary>
        /// Очередь выполнения задач
        /// </summary>
        /// <remarks>
        /// Возможно, лучше было бы сделать три отдельных поля (для повышения понятности кода). 
        /// Изначально был вариант Dictionary<Priority, List<ITask>>, но вариант с массивом должен быстрее работать.
        /// </remarks>
        List<ITask>[] m_TaskQueue = new List<ITask>[] {
            new List<ITask>(),
            new List<ITask>(),
            new List<ITask>(),
        };

        /// <summary>
        /// Количество подряд запущенных задач с высоким приоритетом. 
        /// Необходимо для корректного роутинга
        /// </summary>
        int m_NumberOfHighPriorityTaskInRow = 0;

        /// <summary>
        /// Флаг, что пул активен
        /// </summary>
        bool m_Running = true;

        /// <summary>
        /// Получить следующий таск для обработки
        /// </summary>
        /// <returns></returns>
        internal ITask NextTask()
        {
            lock (m_Lock)
            {
                // если есть таски с высоким и нормальным приоритетом, то пускаем один нормальный таск через каждые 3 приоритетных
                if (m_TaskQueue[(int)Priority.NORMAL].Count > 0 && m_TaskQueue[(int)Priority.HIGH].Count > 0 && m_NumberOfHighPriorityTaskInRow >= 3)
                {
                    m_NumberOfHighPriorityTaskInRow = 0;
                    return TakeFrom(m_TaskQueue[(int)Priority.NORMAL]);
                }

                // далее в порядке приоритета

                if (m_TaskQueue[(int)Priority.HIGH].Count > 0)
                {
                    m_NumberOfHighPriorityTaskInRow++;
                    return TakeFrom(m_TaskQueue[(int)Priority.HIGH]);
                }

                if (m_TaskQueue[(int)Priority.NORMAL].Count > 0)
                {
                    m_NumberOfHighPriorityTaskInRow++;
                    return TakeFrom(m_TaskQueue[(int)Priority.NORMAL]);
                }

                if (m_TaskQueue[(int)Priority.LOW].Count > 0)
                {
                    m_NumberOfHighPriorityTaskInRow++;
                    return TakeFrom(m_TaskQueue[(int)Priority.LOW]);
                }

                // если таска нет
                return null;
            }
        }

        /// <summary>
        /// Извлечь первый элемент из списка и удалить его из списка
        /// </summary>
        /// <param name="a_Queue"></param>
        /// <returns></returns>
        ITask TakeFrom(List<ITask> a_Queue)
        {
            var task = a_Queue[0];
            a_Queue.RemoveAt(0);
            return task;
        }

        /// <summary>
        /// Инициализировать потоки
        /// </summary>
        void InitThreads()
        {
            m_Threads = Enumerable.Range(1, m_WorkerCount).Select(n => new Thread(q =>
            {
                while (true)
                {
                    // определяем следующий таск
                    var task = NextTask();

                    if (task == null)
                    {
                        // если тасков для выполнения нет,
                        // и пул остановлен, завершаем обработку
                        if (!m_Running) break;

                        // иначе ждем
                        Thread.Sleep(1000);
                        continue;                        
                    }
                    
                    task.Execute();
                }
            })
            ).ToArray();

            foreach (var thread in m_Threads) thread.Start();
        }

        #endregion

        /// <summary>
        /// Добавить задачу на выполнение
        /// </summary>
        /// <param name="a_Task">Задача</param>
        /// <param name="a_Priority">Приоритет</param>
        /// <returns></returns>
        public bool Execute(ITask a_Task, Priority a_Priority)
        {
            if (!m_Running) return false;

            lock (m_Lock)
            {
                // потоки инициализируются при первом обращении
                if (m_Threads == null) InitThreads();

                m_TaskQueue[(int)a_Priority].Add(a_Task);
            }

            return true;
        }

        /// <summary>
        /// Остановить пул потоков
        /// </summary>
        public void Stop()
        {
            m_Running = false;
        }
    }
}
