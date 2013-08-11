﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;

namespace battlecity
{
    class Clock
    {
        // Period of each clock cycle in milliseconds
        private long period;

        // Value in milliseconds by which the clock cycle can be bumped forward and backward
        // in order to maintain synchronisation with external events.
        private long deltaPeriod;
        private Thread thread;

        // The master schedule keeps track of the events that have been added to this clock
        private SortedList<long, ThreadStart> masterSchedule;

        // The current schedule is updated per cycle, in order to correctly schedule
        // before-end-of cycle (negative time offset) events for the current cycle's period.
        private SortedList<long, ThreadStart> currentSchedule;

        // true if the current schedule needs to be updated (typically because it hasn't been
        // done before, or because this.balance has changed).
        private bool currentScheduleOutdated;

        // true if the Clock is alive (running or stopped). When false, the clock thread terminates.
        private bool alive;

        // true if the Clock is running. When false, the clock finishes its current cycle and
        // then pauses until running is true again.
        private bool running;

        // 0 if the current clock period should be maintained, -1/+1 if it should be
        // shortened/lengthened by deltaPeriod for one cycle in order to improve phase
        // synchronisation.
        private short delta;

        // Time balance remaining for the current cycle
        private long balance;

        public Clock(long _period = 1000, long _deltaPeriod = 10)
        {
            period = _period;
            deltaPeriod = _deltaPeriod;
            alive = true;
            running = false;
            thread = null;
            currentScheduleOutdated = true;
            masterSchedule = new SortedList<long, ThreadStart>();
            currentSchedule = new SortedList<long, ThreadStart>();
        }

        ~Clock()
        {
            running = false;
            if (thread != null)
                thread.Join();
        }

        private void Run()
        {
            while (alive)
            {
                while (running)
                {
                    /* At the start of each cycle, replicate the master schedule (which includes events
                     * with a time offset relative to the end of the cycle) into the current schedule
                     * for this cycle.
                     */
                    Console.WriteLine("--- TICK ---");
                    if (currentScheduleOutdated)
                    {
                        foreach (KeyValuePair<long, ThreadStart> s in masterSchedule)
                        {
                            if ((s.Key >= 0) && (s.Key <= balance))
                            {
                                currentSchedule.Add(s.Key, s.Value);
                            }
                            else if (s.Key < 0)
                            {
                                if (balance + s.Key > 0)
                                    currentSchedule.Add(balance + s.Key, s.Value);
                                else
                                    currentSchedule.Add(0, s.Value);
                            }
                        }
                        currentScheduleOutdated = false;
                    }

                    long currentTime = 0;
                    long sleepTime = 0;

                    foreach (KeyValuePair<long, ThreadStart> s in currentSchedule)
                    {
                        if (s.Key > currentTime)
                        {
                            sleepTime = s.Key - currentTime;
                            Thread.Sleep((int)sleepTime);
                            currentTime += sleepTime;
                        }

                        Thread t = new Thread(s.Value);
                        t.Start();
                    }

                    sleepTime = balance - deltaPeriod - currentTime;
                    if (sleepTime >= 0)
                        Thread.Sleep((int)sleepTime);
                    else
                        Console.WriteLine("Schedule exceeded current clock cycle's period by {0} ms", -sleepTime);

                    if (delta == 0)
                    {
                        /* delta is interpreted in a trinary fashion. If it's negative, the clock should pull
                         * back by deltaPeriod (i.e. dont sleep any more this round). If it's 0, sleep for
                         * deltaPeriod to get to the full period (qf. the initialisation of balance). It it's
                         * positive, sleep for an additional deltaPeriod.
                         */
                        System.Threading.Thread.Sleep((int)deltaPeriod);
                    }
                    else if (delta > 0)
                    {
                        System.Threading.Thread.Sleep(2*(int)deltaPeriod);
                    }
                }
            }
        }

        public void Start(long initialPeriod = 0)
        {
            if (initialPeriod > 0)
                balance = period;
            else
                balance = initialPeriod;
            currentScheduleOutdated = true;
            thread = new Thread(Run);
            running = true;
            thread.Start();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public void Reset(long delay = 0, bool applySchedule = true)
        {
            /* Reset the clock, and specify the delay before the next timer clock cycle
             * starts. If applySchedule is true, tasks are executed as scheduled during
             * the delay period: tasks with positive time stamps are executed after the
             * scheduled period, and may be dropped if the delay period is too short. Tasks
             * with negative time stamps are guaranteed to run.
             */
            currentScheduleOutdated = true;
            throw new NotImplementedException();
        }

        public void AddTask(long time, ThreadStart callBack)
        {
            /* Add a task to the clock's schedule. Tasks can be scheduled with a positive time
             * stamp, indicating the delay (in milliseconds) after the start of the period at
             * which that task should fire. A negative time stamp specifies the period (in
             * milliseconds) before the end of the current period at which the task should fire.
             * 
             * Tasks with negative time stamps are guaranteed to fire each period, even when Reset()
             * is called with a delay smaller than the full period.
             */
            if (time > period)
                throw new ArgumentOutOfRangeException(String.Format(
                    "Time scheduled for the task is greater than the clock period ({0} ms)",
                    period));
            if ((time < 0) && (time > -deltaPeriod))
                throw new ArgumentOutOfRangeException(String.Format(
                    "Before-cycle-end tasks may not fall within the delta period (-{0} ms).",
                    deltaPeriod));
            masterSchedule.Add(time, callBack);
        }
    }
}