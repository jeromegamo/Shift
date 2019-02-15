alter table Item
drop constraint [FK_ItemCategory_Item]

truncate table ItemCategory

alter table Item
add constraint [FK_ItemCategory_Item] foreign key (Category)
    references ItemCategory(Name)