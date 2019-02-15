create table Player(
    Id bigint identity(1,1),
    Name varchar(50) not null,
    constraint PK_Player primary key (Id)
)