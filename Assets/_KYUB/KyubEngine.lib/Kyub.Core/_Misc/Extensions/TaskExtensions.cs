using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Collections;
using System.Threading.Tasks;
using System;
using System.Threading;

namespace Kyub.Extensions
{
    public static class TaskExtensions
    {
        public static Task ContinueWithMainThread(this Task task, Action<Task, object> continuationAction, object state)
        {
            return task.ContinueWith((internalResult, internalState) =>
            {
                ApplicationContext.RunOnMainThread(() =>
                {
                    continuationAction?.Invoke(internalResult, internalState);
                });
            }, state);
        }

        public static Task ContinueWithMainThread(this Task task, Action<Task, object> continuationAction, object state, CancellationToken cancellationToken)
        {
            return task.ContinueWith((internalResult, internalState) =>
            {
                ApplicationContext.RunOnMainThread(() =>
                {
                    continuationAction?.Invoke(internalResult, internalState);
                });
            }, state, cancellationToken);
        }

        public static Task ContinueWithMainThread(this Task task, Action<Task, object> continuationAction, object state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return task.ContinueWith((internalResult, internalState) =>
            {
                ApplicationContext.RunOnMainThread(() =>
                {
                    continuationAction?.Invoke(internalResult, internalState);
                });
            }, state, cancellationToken, continuationOptions, scheduler);
        }

        public static Task ContinueWithMainThread(this Task task, Action<Task, object> continuationAction, object state, TaskContinuationOptions continuationOptions)
        {
            return task.ContinueWith((internalResult, internalState) =>
            {
                ApplicationContext.RunOnMainThread(() =>
                {
                    continuationAction?.Invoke(internalResult, internalState);
                });
            }, state, continuationOptions);
        }

        public static Task ContinueWithMainThread(this Task task, Action<Task, object> continuationAction, object state, TaskScheduler scheduler)
        {
            return task.ContinueWith((internalResult, internalState) =>
            {
                ApplicationContext.RunOnMainThread(() =>
                {
                    continuationAction?.Invoke(internalResult, internalState);
                });
            }, state, scheduler);
        }

        public static Task ContinueWithMainThread(this Task task, Action<Task> continuationAction)
        {
            return task.ContinueWith((internalResult) =>
            {
                ApplicationContext.RunOnMainThread(() =>
                {
                    continuationAction?.Invoke(internalResult);
                });
            });
        }

        public static Task ContinueWithMainThread(this Task task, Action<Task> continuationAction, CancellationToken cancellationToken)
        {
            return task.ContinueWith((internalResult, internalState) =>
            {
                ApplicationContext.RunOnMainThread(() =>
                {
                    continuationAction?.Invoke(internalResult);
                });
            }, cancellationToken);
        }

        public static Task ContinueWithMainThread(this Task task, Action<Task> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            return task.ContinueWith((internalResult) =>
            {
                ApplicationContext.RunOnMainThread(() =>
                {
                    continuationAction?.Invoke(internalResult);
                });
            }, cancellationToken, continuationOptions, scheduler);
        }

        public static Task ContinueWithMainThread(this Task task, Action<Task> continuationAction, TaskContinuationOptions continuationOptions)
        {
            return task.ContinueWith((internalResult) =>
            {
                ApplicationContext.RunOnMainThread(() =>
                {
                    continuationAction?.Invoke(internalResult);
                });
            }, continuationOptions);
        }
    }
}