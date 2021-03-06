﻿// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Registration.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Infrastructure.Processes;
    using Payments.Contracts.Events;
    using Registration.Events;
    using Xunit;

    public class RegistrationProcessRouterFixture
    {
        [Fact]
        public void when_order_placed_then_routes_and_saves()
        {
            var context = new StubProcessDataContext<RegistrationProcess>();
            var router = new RegistrationProcessRouter(() => context);

            router.Handle(new OrderPlaced { SourceId = Guid.NewGuid(), ConferenceId = Guid.NewGuid(), Seats = new SeatQuantity[0] });

            Assert.Equal(1, context.SavedProcesses.Count);
            Assert.True(context.DisposeCalled);
        }

        [Fact]
        public void when_order_updated_then_routes_and_saves()
        {
            var process = new RegistrationProcess
            {
                State = RegistrationProcess.ProcessState.AwaitingReservationConfirmation,
                ReservationId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var context = new StubProcessDataContext<RegistrationProcess> { Store = { process } };
            var router = new RegistrationProcessRouter(() => context);

            router.Handle(new OrderUpdated { SourceId = process.OrderId, Seats = new SeatQuantity[0] });

            Assert.Equal(1, context.SavedProcesses.Count);
            Assert.True(context.DisposeCalled);
        }

        [Fact]
        public void when_reservation_accepted_then_routes_and_saves()
        {
            var process = new RegistrationProcess
            {
                State = RegistrationProcess.ProcessState.AwaitingReservationConfirmation,
                ReservationId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var context = new StubProcessDataContext<RegistrationProcess> { Store = { process } };
            var router = new RegistrationProcessRouter(() => context);

            router.Handle(new SeatsReserved { SourceId = process.ConferenceId, ReservationId = process.ReservationId, ReservationDetails = new SeatQuantity[0] });

            Assert.Equal(1, context.SavedProcesses.Count);
            Assert.True(context.DisposeCalled);
        }

        [Fact]
        public void when_payment_received_then_routes_and_saves()
        {
            var process = new RegistrationProcess
            {
                State = RegistrationProcess.ProcessState.ReservationConfirmationReceived,
                OrderId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10),
            };
            var context = new StubProcessDataContext<RegistrationProcess> { Store = { process } };
            var router = new RegistrationProcessRouter(() => context);

            router.Handle(new PaymentCompleted { PaymentSourceId = process.OrderId });

            Assert.Equal(1, context.SavedProcesses.Count);
            Assert.True(context.DisposeCalled);
        }

        [Fact]
        public void when_order_confirmed_received_then_routes_and_saves()
        {
            var process = new RegistrationProcess
            {
                State = RegistrationProcess.ProcessState.PaymentConfirmationReceived,
                OrderId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10),
            };
            var context = new StubProcessDataContext<RegistrationProcess> { Store = { process } };
            var router = new RegistrationProcessRouter(() => context);

            router.Handle(new OrderConfirmed { SourceId = process.OrderId });

            Assert.Equal(1, context.SavedProcesses.Count);
            Assert.True(context.DisposeCalled);
        }

        [Fact]
        public void when_order_expired_then_routes_and_saves()
        {
            var process = new RegistrationProcess
            {
                State = RegistrationProcess.ProcessState.AwaitingReservationConfirmation,
                ReservationId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                ReservationAutoExpiration = DateTime.UtcNow.AddMinutes(10)
            };
            var context = new StubProcessDataContext<RegistrationProcess> { Store = { process } };

            var router = new RegistrationProcessRouter(() => context);

            router.Handle(new Commands.ExpireRegistrationProcess { ProcessId = process.Id });

            Assert.Equal(1, context.SavedProcesses.Count);
            Assert.True(context.DisposeCalled);
        }
    }

    class StubProcessDataContext<T> : IProcessDataContext<T> where T : class, IProcess
    {
        public readonly List<T> SavedProcesses = new List<T>();

        public readonly List<T> Store = new List<T>();

        public bool DisposeCalled { get; set; }

        public T Find(Guid id)
        {
            return this.Store.SingleOrDefault(x => x.Id == id);
        }

        public void Save(T process)
        {
            this.SavedProcesses.Add(process);
        }

        public T Find(Expression<Func<T, bool>> predicate)
        {
            return this.Store.AsQueryable().SingleOrDefault(predicate);
        }

        public void Dispose()
        {
            this.DisposeCalled = true;
        }
    }
}
