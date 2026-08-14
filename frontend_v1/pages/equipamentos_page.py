from components.crud_page import CrudPage


def criar_equipamentos_page(
    page,
    api,
):
    return CrudPage(
        page=page,
        api=api,

        titulo="Equipamentos",

        id_campo="idEquipamento",

    colunas=[
        ("ID", "idEquipamento"),
        ("Cliente", "idCliente"),
        ("Tipo", "tipo"),
        ("Marca", "marca"),
],

)