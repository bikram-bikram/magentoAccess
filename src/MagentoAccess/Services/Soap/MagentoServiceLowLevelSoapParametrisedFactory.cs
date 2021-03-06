﻿using System;
using System.Collections.Generic;
using System.Linq;
using MagentoAccess.Models.Credentials;
using MagentoAccess.Services.Soap._1_7_0_1_ce_1_9_0_1_ce;
using MagentoAccess.Services.Soap._1_9_2_1_ce;

namespace MagentoAccess.Services.Soap
{
	internal class MagentoServiceLowLevelSoapFactory
	{
		private readonly Dictionary< string, IMagentoServiceLowLevelSoap > _factories;
		private readonly MagentoAuthenticatedUserCredentials _magentoAuthenticatedUserCredentials;

		public MagentoServiceLowLevelSoapFactory( MagentoAuthenticatedUserCredentials magentoAuthenticatedUserCredentials, Dictionary< string, IMagentoServiceLowLevelSoap > factories )
		{
			this._magentoAuthenticatedUserCredentials = magentoAuthenticatedUserCredentials;
			this._factories = factories;
		}

		public IEnumerable< KeyValuePair< string, IMagentoServiceLowLevelSoap > > GetAll()
		{
			return this._factories.ToList();
		}

		public IMagentoServiceLowLevelSoap GetMagentoServiceLowLevelSoap( string magentoVersion, bool tryToSelectSuitable, bool returnNullIfNoSuitable )
		{
			Func< Dictionary< string, IMagentoServiceLowLevelSoap >, string, IMagentoServiceLowLevelSoap > getMagentoLowLevelServiceAndConfigureIt = ( facts, ver ) =>
			{
				var magentoServiceLowLevelSoap = facts[ ver ];
				magentoServiceLowLevelSoap.StoreVersion = ver;
				return magentoServiceLowLevelSoap;
			};

			var factories = this._factories.OrderBy( x => x.Key ).ToDictionary( x => x.Key, y => y.Value );
			factories.Add( "1.9.3.x", new MagentoServiceLowLevelSoap_v_1_9_2_1_ce_Factory().CreateMagentoLowLevelService( this._magentoAuthenticatedUserCredentials ) );
			if( tryToSelectSuitable && !factories.ContainsKey( magentoVersion ) )
			{
				// try to use similar version
				for( var j = 0; j < magentoVersion.Length; j++ )
				{
					var versions = factories.Keys.Where( x => x.Substring( 0, x.Length - j ) == magentoVersion.Substring( 0, magentoVersion.Length - j ) );
					var versionList = versions as IList< string > ?? versions.ToList();
					if( versionList.Any() )
					{
						factories.Add( magentoVersion, factories[ versionList.First() ] );
						return getMagentoLowLevelServiceAndConfigureIt( factories, magentoVersion );
					}
				}

				// there is no suitable
				if( returnNullIfNoSuitable )
					return null;

				// try to use 1.7- 1.9 low level service if can't detect version
				new MagentoServiceLowLevelSoap_v_1_7_to_1_9_0_1_CE_Factory().CreateMagentoLowLevelService( this._magentoAuthenticatedUserCredentials );
				this._factories.Add( magentoVersion, new MagentoServiceLowLevelSoap_v_1_7_to_1_9_0_1_CE_Factory().CreateMagentoLowLevelService( this._magentoAuthenticatedUserCredentials ) );
				factories.Add( magentoVersion, new MagentoServiceLowLevelSoap_v_1_7_to_1_9_0_1_CE_Factory().CreateMagentoLowLevelService( this._magentoAuthenticatedUserCredentials ) );
			}
			return getMagentoLowLevelServiceAndConfigureIt( factories, magentoVersion );
		}

		public string GetSubVersion( int deep, string magentoVer )
		{
			return magentoVer.Split( '.' )[ deep ];
		}
	}
}