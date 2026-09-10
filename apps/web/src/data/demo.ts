// Motivo: fixtures fechadas não são pedidos, tarifas, prazos ou regras operacionais.
// Não persistir nem misturar estes objetos com DTOs da futura API.
// Mudança: docs/mudancas/2026-09-10-01-interface-operacional.md
export interface DeliveryExample {
  id: string;
  pickup: string;
  destination: string;
  district: string;
  status: "Aguardando coleta" | "Em rota" | "Entregue";
  fee: number;
  distance: string;
  volumes: number;
}
export const demoDeliveries: readonly DeliveryExample[] = [
  {
    id: "EX-1042",
    pickup: "Café da Esquina · Rua das Flores, 120",
    destination: "Rua do Parque, 85",
    district: "Jardim",
    status: "Aguardando coleta",
    fee: 12.5,
    distance: "3,4",
    volumes: 1,
  },
  {
    id: "EX-1041",
    pickup: "Empório Central · Rua das Acácias, 40",
    destination: "Avenida das Palmeiras, 230",
    district: "Centro",
    status: "Em rota",
    fee: 15,
    distance: "4,2",
    volumes: 2,
  },
  {
    id: "EX-1040",
    pickup: "Floricultura Primavera · Rua das Rosas, 18",
    destination: "Rua do Sol, 54",
    district: "Vila Nova",
    status: "Entregue",
    fee: 10,
    distance: "2,1",
    volumes: 1,
  },
  {
    id: "EX-1039",
    pickup: "Livraria do Bairro · Rua dos Ipês, 90",
    destination: "Rua da Praça, 12",
    district: "Centro",
    status: "Aguardando coleta",
    fee: 11.5,
    distance: "2,8",
    volumes: 1,
  },
  {
    id: "EX-1038",
    pickup: "Padaria Amanhecer · Rua do Lago, 15",
    destination: "Rua das Mangueiras, 77",
    district: "Jardim",
    status: "Em rota",
    fee: 14,
    distance: "3,9",
    volumes: 2,
  },
  {
    id: "EX-1037",
    pickup: "Ateliê Criativo · Rua das Artes, 20",
    destination: "Rua das Orquídeas, 200",
    district: "Vila Nova",
    status: "Entregue",
    fee: 13,
    distance: "3,2",
    volumes: 1,
  },
];
export const statusFilters = [
  "Todas",
  "Aguardando coleta",
  "Em rota",
  "Entregue",
] as const;
export const formatMoney = (value: number) =>
  new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(
    value,
  );
