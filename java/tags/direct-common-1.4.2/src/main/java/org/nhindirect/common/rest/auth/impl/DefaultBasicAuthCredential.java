/* 
Copyright (c) 2010, NHIN Direct Project
All rights reserved.

Authors:
   Greg Meyer      gm2552@cerner.com
 
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer 
in the documentation and/or other materials provided with the distribution.  Neither the name of the The NHIN Direct Project (nhindirect.org). 
nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, 
THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS 
BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, 
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
THE POSSIBILITY OF SUCH DAMAGE.
*/

package org.nhindirect.common.rest.auth.impl;

import org.nhindirect.common.rest.auth.BasicAuthCredential;

/**
 * Default implementation of a BasicAuth credential that boostraps all attributes and are immutable.
 * @author Greg Meyer
 * @since 1.3
 */
public class DefaultBasicAuthCredential implements BasicAuthCredential
{
	protected final String name;
	
	protected final String password;
	
	protected final String role;
	
	/**
	 * Constructor setting all attributes
	 * @param name The username/principal.
	 * @param password The password.
	 * @param role The roll.
	 */
	public DefaultBasicAuthCredential(String name, String password, String role)
	{
		this.name = name;
		this.password = password;
		this.role = role;
	}

	/**
	 * {@inheritDoc}
	 */
	@Override
	public String getUser() 
	{
		return name;
	}

	/**
	 * {@inheritDoc}
	 */
	@Override
	public String getPassword() 
	{
		return password;
	}

	/**
	 * {@inheritDoc}
	 */
	@Override
	public String getRole() 
	{
		return role;
	}
	
	
}
