﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CachingFramework.Redis.UnitTest
{
    [TestClass]
    public class UnitTestPubSub
    {
        private static Context _context;
        private string _defaultConfig;
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            _context = Common.GetContextAndFlush();
        }

        [TestMethod]
        public void UT_PubSub_SingleSubscribe()
        {
            var ch = "UT_PubSub_SingleSubscribe";
            var users = GetUsers();
            var usersList = new List<User>();
            _context.PubSub.Subscribe<User>(ch, (c, o) => usersList.Add(o));
            foreach (var t in users)
            {
                _context.PubSub.Publish(ch, t);
            }
            Thread.Sleep(500);
            Assert.AreEqual(users.Count, usersList.Count);
            Assert.IsTrue(users.All(u => usersList.Any(ul => ul.Id == u.Id)));
            _context.PubSub.Unsubscribe(ch);
        }

        [TestMethod]
        public void UT_PubSub_SingleUnsubscribe()
        {
            var ch = "UT_PubSub_SingleUnsubscribe";
            var users = GetUsers();
            var usersList = new List<User>();
            _context.PubSub.Subscribe<User>(ch, (c, o) => usersList.Add(o));
            foreach (var t in users)
            {
                _context.PubSub.Publish(ch, t);
            }
            Thread.Sleep(500);
            Assert.AreEqual(users.Count, usersList.Count);
            _context.PubSub.Unsubscribe(ch);
            _context.PubSub.Publish(ch, users[0]);
            Assert.AreEqual(users.Count, usersList.Count);
        }

        [TestMethod]
        public void UT_PubSub_SubscribeMultipleTypes()
        {
            var ch = "UT_PubSub_SingleUnsubscribe";
            var users = GetUsers();
            int objCount = 0;
            int iDtoCount = 0;
            _context.PubSub.Subscribe<object>(ch, (c, o) => objCount++);
            _context.PubSub.Subscribe<IDto>(ch, (c, o) => iDtoCount++);
            foreach (var t in users)
            {
                _context.PubSub.Publish(ch, t);
            }
            _context.PubSub.Publish(ch, new Exception("a different object type"));
            _context.PubSub.Publish(ch, users[0].Deparments[0]);
            _context.PubSub.Publish(ch, "some string");
            Thread.Sleep(500);
            Assert.AreEqual(users.Count + 3, objCount);
            Assert.AreEqual(users.Count + 1, iDtoCount);
            _context.PubSub.Unsubscribe(ch);
        }

        [TestMethod]
        public void UT_PubSub_SubscribeWilcards()
        {
            var ch = "UT_PubSub_SubscribeWilcards";
            var users = GetUsers();
            var channels = new List<string>();
            var objects = new List<User>();
            _context.PubSub.Subscribe<User>(ch + ".*", (c, o) =>
            {
                channels.Add(c);
                objects.Add(o);
            });
            int user0count = 0;
            _context.PubSub.Subscribe<User>(ch + ".user0", (c, o) =>
            {
                user0count++;
            });
            _context.PubSub.Publish(ch + ".user0", users[0]);
            _context.PubSub.Publish(ch + ".user1", users[1]);
            Thread.Sleep(500);
            Assert.AreEqual(2, channels.Count);
            Assert.AreEqual(users[0].Id, objects[0].Id);
            Assert.AreEqual(users[1].Id, objects[1].Id);
            Assert.AreEqual(1, user0count);

            _context.PubSub.Unsubscribe(ch + ".*");
            Thread.Sleep(1500);
            _context.PubSub.Publish(ch + ".user2", users[2]);
            Assert.AreEqual(2, channels.Count);
            Assert.AreEqual(1, user0count);
        }

        private List<User> GetUsers()
        {
            var loc1 = new Location()
            {
                Id = 1,
                Name = "one"
            };
            var loc2 = new Location()
            {
                Id = 2,
                Name = "two"
            };
            var user1 = new User()
            {
                Id = 1,
                Deparments = new List<Department>()
                {
                    new Department() {Id = 1, Distance = 123.45m, Size = 2, Location = loc1},
                    new Department() {Id = 2, Distance = 400, Size = 1, Location = loc2}
                }
            };
            var user2 = new User()
            {
                Id = 2,
                Deparments = new List<Department>()
                {
                    new Department() {Id = 3, Distance = 500, Size = 1, Location = loc2},
                    new Department() {Id = 4, Distance = 125.5m, Size = 3, Location = loc1}
                }
            };
            var user3 = new User()
            {
                Id = 3,
                Deparments = new List<Department>()
                {
                    new Department() {Id = 5, Distance = 5, Size = 5, Location = loc2},
                }
            };
            var user4 = new User()
            {
                Id = 4,
                Deparments = new List<Department>()
                {
                    new Department() {Id = 6, Distance = 100, Size = 10, Location = loc1},
                }
            };
            return new List<User>() { user1, user2, user3, user4 };
        }
    }
}
