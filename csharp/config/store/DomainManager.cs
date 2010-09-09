﻿/* 
 Copyright (c) 2010, NHIN Direct Project
 All rights reserved.

 Authors:
    Umesh Madan     umeshma@microsoft.com
  
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of the The NHIN Direct Project (nhindirect.org). nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NHINDirect.Config.Store
{
    public class DomainManager : IEnumerable<Domain>
    {
        ConfigStore m_store;

        internal DomainManager(ConfigStore store)
        {
            m_store = store;
        }

        internal ConfigStore Store
        {
            get
            {
                return m_store;
            }
        }
        
        public void Add(string name)
        {
            using(ConfigDatabase db = this.Store.CreateContext())
            {
                this.Add(db, name);
                db.SubmitChanges();
            }
        }
        
        public void Add(ConfigDatabase db, string name)
        {
            if (db == null)
            {
                throw new ArgumentNullException("db");
            }
            
            db.Domains.InsertOnSubmit(new Domain(name));
        }

        public void Add(Domain domain)
        {
            using (ConfigDatabase db = this.Store.CreateContext())
            {
                this.Add(db, domain);
                db.SubmitChanges();
            }
        }

        public void Add(ConfigDatabase db, Domain domain)
        {
            if (db == null)
            {
                throw new ArgumentNullException("db");
            }
            if (domain == null)
            {
                throw new ConfigStoreException(ConfigStoreError.InvalidDomain);
            }
            
            if (!domain.IsValidEmailDomain())
            {
                throw new ConfigStoreException(ConfigStoreError.InvalidDomain);
            }
            
            db.Domains.InsertOnSubmit(domain);
        }
        
        public int Count()
        {
            using (ConfigDatabase db = this.Store.CreateReadContext())
            {
                return db.Domains.GetCount();
            }            
        }
                
        public Domain Get(string name)
        {
            using (ConfigDatabase db = this.Store.CreateReadContext())
            {
                return this.Get(db, name);
            }
        }

        public Domain Get(ConfigDatabase db, string name)
        {
            if (db == null)
            {
                throw new ArgumentNullException("db");
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ConfigStoreException(ConfigStoreError.InvalidDomainName);
            }
            
            return db.Domains.Get(name);
        }
        
        public Domain[] Get(string[] names)
        {
            return this.Get(names, null);
        }
                
        public IEnumerable<Domain> Get(ConfigDatabase db, string[] names)
        {
            return this.Get(db, names, null);
        }

        public Domain[] Get(string[] names, EntityStatus? status)
        {
            using (ConfigDatabase db = this.Store.CreateReadContext())
            {
                return this.Get(db, names, status).ToArray();
            }
        }
        
        public IEnumerable<Domain> Get(ConfigDatabase db, string[] names, EntityStatus? status)
        {
            if (db == null)
            {
                throw new ArgumentNullException("db");
            }
            if (names.IsNullOrEmpty())
            {
                throw new ConfigStoreException(ConfigStoreError.InvalidDomainName);
            }
            
            if (status == null)
            {
                return db.Domains.Get(names);
            }
            
            return db.Domains.Get(names, status.Value);
        }

        public Domain[] Get(string lastDomain, int maxResults)
        {
            using (ConfigDatabase db = this.Store.CreateReadContext())
            {
                return this.Get(db, lastDomain, maxResults).ToArray();
            }
        }

        public IEnumerable<Domain> Get(ConfigDatabase db, string lastDomain, int maxResults)
        {
            if (db == null)
            {
                throw new ArgumentNullException();
            }

            return db.Domains.ExecGet(lastDomain, maxResults);
        }

        public void Update(Domain domain)
        {
            using (ConfigDatabase db = this.Store.CreateContext())
            {
                this.Update(db, domain);
                db.SubmitChanges();
            }
        }

        public void Update(ConfigDatabase db, Domain domain)
        {
            if (db == null)
            {
                throw new ArgumentNullException("db");
            }
            if (domain == null)
            {
                throw new ConfigStoreException(ConfigStoreError.InvalidDomain);
            }
            
            Domain update = new Domain();
            update.CopyFixed(domain);
                        
            db.Domains.Attach(update);
            update.ApplyChanges(domain);
        }
                
        public void Remove(string name)
        {
            using(ConfigDatabase db = this.Store.CreateContext())
            {
                this.Remove(db, name);
            }
        }

        public void Remove(ConfigDatabase db, string name)
        {
            if (db == null)
            {
                throw new ArgumentNullException();
            }
            
            if (string.IsNullOrEmpty(name))
            {
                throw new ConfigStoreException(ConfigStoreError.InvalidDomainName);
            }
            
            db.Domains.ExecDelete(name);
        }

        public IEnumerator<Domain> GetEnumerator()
        {
           using(ConfigDatabase db = this.Store.CreateContext())
           {
                foreach(Domain domain in db.Domains)
                {
                    yield return domain;
                }       
           }
        }

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}
