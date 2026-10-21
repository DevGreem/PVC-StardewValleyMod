# Spec Delta

## MODIFIED Requirements

### Requirement: Proximity radius is validated

The mod SHALL discard a packet whose declared proximity radius is neither one of the current offered values (`3`, `15`, `30`) nor one of the legacy values (`6`, `10`, `20`) accepted for compatibility. The sender SHALL only advertise current offered values.

#### Scenario: Unsupported radius declared

- **WHEN** a packet declares a proximity radius outside both the current and legacy sets
- **THEN** the packet is discarded

#### Scenario: Current radius declared

- **WHEN** a packet declares `3`, `15` or `30`
- **THEN** the packet is accepted for proximity evaluation

#### Scenario: Legacy radius declared

- **WHEN** a packet declares `6`, `10` or `20` from an older build
- **THEN** the packet is accepted for proximity evaluation instead of being discarded
