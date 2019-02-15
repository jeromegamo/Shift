create table ItemCategory(
    Name varchar(50) not null,
    constraint PK_ItemCategory primary key (Name)
)

create table Item(
    Id bigint identity(1,1),
    Name varchar(50) not null,
    Category varchar(50) not null,
    constraint PK_Item primary key (Id),
    constraint FK_ItemCategory_Item foreign key (Category)
    references ItemCategory(Name)
)