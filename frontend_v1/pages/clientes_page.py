"""
Configuração da página de Clientes - V1.

A lógica continua concentrada em CrudPage.
Clientes informa somente:
    título
    campo ID
    colunas/campos

V1:
    os mesmos campos também serão usados pelo formulário de edição.
"""

from components.crud_page import CrudPage


def criar_clientes_page(page, api):
    return CrudPage(
        page=page,
        api=api,
        titulo="Clientes",
        id_campo="idCliente",
        colunas=[
            ("ID", "idCliente"),
            ("NOME", "nome"),
            ("TELEFONE", "telefone"),
            ("EMAIL", "email"),
        ],
    )
