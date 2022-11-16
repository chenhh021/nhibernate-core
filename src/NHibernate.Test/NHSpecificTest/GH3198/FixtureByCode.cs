﻿using System;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Event;
using NHibernate.Mapping.ByCode;
using NHibernate.Type;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.GH3198
{
	/// <summary>
	/// Fixture using 'by code' mappings
	/// </summary>
	/// <remarks>
	/// This fixture is identical to <see cref="Fixture" /> except the <see cref="Entity" /> mapping is performed 
	/// by code in the GetMappings method, and does not require the <c>Mappings.hbm.xml</c> file. Use this approach
	/// if you prefer.
	/// </remarks>
	[TestFixture]
	public partial class ByCodeFixture : TestCaseMappingByCode
	{
		private static readonly int EXAMPLE_ID_VALUE = 1;
		private readonly testEventListener listener = new testEventListener();

		protected override void Configure(Configuration configuration)
		{
			// A listener always returning true
			configuration.EventListeners.PreUpdateEventListeners = new IPreUpdateEventListener[]
			{
				listener
			};
			base.Configure(configuration);
		}
		protected override HbmMapping GetMappings()
		{
			var mapper = new ModelMapper();
			mapper.Class<Entity>(rc =>
			{
				rc.Table("Entity");
				rc.Id(x => x.id, m => m.Generator(Generators.Assigned));
				rc.Property(x => x.name, x=>x.Type<StringType>());
				rc.Version(x => x.Version, vm => { });
			});

			return mapper.CompileMappingForAllExplicitlyAddedEntities();
		}

		protected override void OnSetUp()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var e1 = new Entity { id = EXAMPLE_ID_VALUE, name = "old_name"  };
				session.Save(e1);
				transaction.Commit();
			}
		}

		protected override void OnTearDown()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				session.CreateQuery("delete from System.Object").ExecuteUpdate();

				transaction.Commit();
			}
		}

		[Test]
		public void testVersionNotChangedWhenPreUpdateEventVetod()
		{
			using (var session = OpenSession())
			{
				var entity = session.Load<Entity>(EXAMPLE_ID_VALUE);

				entity.name = "new_name";
				session.Update(entity);
				
				var versionBeforeFlush = entity.Version;
				
				session.Flush();

				var versionAfterflush = entity.Version;
				
				Assert.That(versionAfterflush, Is.EqualTo(versionBeforeFlush), "The entity version must not change when update is vetoed");
			}
		}
		
		// A listener always returning true
		public partial class testEventListener : IPreUpdateEventListener
		{
			public static testEventListener Instance = new testEventListener();

			public bool Executed { get; set; }

			public bool FoundAny { get; set; }
			public bool OnPreUpdate(PreUpdateEvent @event)
			{
				return true;
			}
			
		}
		public partial class Entity
		{
			public virtual int id { get; set; }
			public virtual string name { get; set; }
			public virtual int Version { get; set; }
		}
	}
}
