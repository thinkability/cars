﻿// The MIT License (MIT)
// 
// Copyright (c) 2016 Nelson Corrêa V. Júnior
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cars.Events;
using Cars.Handlers;
using Moq;

namespace Cars.Testing.Shared.MessageBus
{
    public abstract class EventTestFixture<TEvent, TEventHandler>
        where TEvent : class, IDomainEvent
        where TEventHandler : class, IEventHandler<TEvent>
    {
        private readonly IDictionary<Type, object> _mocks;

        protected Exception CaughtException;
        protected TEventHandler EventHandler;
        protected virtual void SetupDependencies() { }
        protected abstract TEvent When();
        protected virtual void Finally() { }

        protected EventTestFixture()
        {
            _mocks = new Dictionary<Type, object>();
            CaughtException = new ThereWasNoExceptionButOneWasExpectedException();
            EventHandler = BuildHandler();
            SetupDependencies();

            try
            {
                EventHandler.ExecuteAsync(When()).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                CaughtException = exception;
            }
            finally
            {
                Finally();
            }
        }

        public Mock<TType> OnDependency<TType>() where TType : class
        {
            if (!_mocks.ContainsKey(typeof(TType)))
                throw new Exception($"The event handler '{typeof(TEventHandler).FullName}' does not have a dependency upon '{typeof(TType).FullName}'");

            return (Mock<TType>)_mocks[typeof(TType)];
        }

        private TEventHandler BuildHandler()
        {
            var constructorInfo = typeof(TEventHandler).GetConstructors().First();

            foreach (var parameter in constructorInfo.GetParameters())
            {
                if (parameter.ParameterType.Name.EndsWith("Repository"))
                {
                    var mockType = typeof(Mock<>).MakeGenericType(parameter.ParameterType);

                    var repositoryMock = (Mock)Activator.CreateInstance(mockType);
                    _mocks.Add(parameter.ParameterType, repositoryMock);
                    continue;
                }

                _mocks.Add(parameter.ParameterType, CreateMock(parameter.ParameterType));
            }

            return (TEventHandler)constructorInfo.Invoke(_mocks.Values.Select(x => ((Mock)x).Object).ToArray());
        }

        private static object CreateMock(Type type)
        {
            var constructorInfo = typeof(Mock<>).MakeGenericType(type).GetConstructors().First();
            return constructorInfo.Invoke(new object[] { });
        }
    }
}