DbDiver
=======

DbDiver is a tool for exploring the schema and data that comprise a database. It
is not an outright replacement for other, more general tools. Instead, its
features are intended to be convenient for the specific task of becoming
familiar with a new schema or keeping track of an old, hairy beast.

There are displays to: list the tables in the database and their sizes, and its
stored procedures and functions; explore the relationships between tables in a
visual manner; lookup the definitions of stored procedures and functions; browse
and edit data in specific tables; and run ad-hoc queries.

All of that's standard fair, and entirely possible from standard tools, but
DbDiver is designed to provide those specific activities in a simple and quick
manner. In addition, it has several small features that are nice to have when
exploring unfamiliar territory, like a find tool that can search over query
results (or anywhere else in the application), and sorting and column re-ordering
after-the-fact.


Supported Databases Systems
---------------------------

At the moment, DbDiver supports Microsoft Sql Server (including network server
discovery and integrated authentication), Sql Server Compact Edition, and MySQL.


TODO
----

* Need a method for editing table data in place (mostly done).
	* It would be nice if adding a row with FKs provided a way to search for
	  appropriate foreign rows.
* Oracle, MySQL, and PostgreSQL support.
	* MySQL is now supported.


Copying
-------

DbDiver is copyright Ray Wallace, 2014. You may copy this software under the
terms of the GNU Public License, version 2.  See COPYING for details.
