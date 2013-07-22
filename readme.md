DbDiver
=======

DbDiver is a tool for exploring schema and data that comprise a database. It is
not an outright replacement for other, more general tools. Instead, its features
are intended to be convenient for the specific task of becoming familiar with a
new schema or keeping track of an old, hairy beast.

There are displays to: list the tables in the database and their sizes, and its
stored procedures and functions; explore the relationships between tables in a
visual manner; lookup the definitions of stored procedures and functions; and
run ad-hoc queries.

All of that's standard fair, and entirely possible from standard tools, but
DbDiver is designed to provide those specific activities in a simple and quick
manner. In addition, it has several small features that are nice to have when
exploring unfamiliar territory, like a find tool that can search over query
results (or anywhere else in the application), and sorting and column re-ordering
after-the-fact.


Supported Databases Systems
---------------------------

At the moment, DbDiver supports Microsoft Sql Server (including network server
discovery and integrated authentication) and Sql Server Compact Edition.


TODO
----

* Find in the Lookup tab is broken; a proper implementation is unfortunately a
  bit complicated.
* Need a method for editing table data in place.
* Need a method for crawling the relationships between concrete rows of data.
* Oracle, MySQL, and PostgreSQL support.
* Improve handling of login errors.


Copying
-------

DbDiver is copyright Ray Wallace, 2013. You may copy this software under the
terms of the GNU Public License, version 2.  See COPYING for details.
